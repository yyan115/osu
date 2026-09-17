#!/usr/bin/env bash
set -euo pipefail
: "${GH_TOKEN:?GitHub token is required}"
: "${GH_REPO:?Repository is required}"
[[ "$GH_REPO" == yyan115/osu ]] || { echo 'Wrong repository'; exit 1; }
source_sha="${PHANTOM_SOURCE_SHA:-$GITHUB_SHA}"
source_run="${PHANTOM_SOURCE_RUN:-$GITHUB_RUN_ID}"
[[ "$source_sha" =~ ^[0-9a-f]{40}$ && "$source_run" =~ ^[0-9]+$ ]]
work=$(mktemp -d)
trap 'rm -rf "$work"' EXIT

# Confirm that both archives came from the intended tested source and have the same version.
(cd dist && sha256sum -c ./*.sha256)
linux_sha=$(tar -xOf dist/phantom-misses-linux-x64.tar.gz phantom-misses-linux-x64/COMMIT.txt | tr -d '\r\n')
windows_sha=$(unzip -p dist/phantom-misses-win-x64.zip phantom-misses-win-x64/COMMIT.txt | tr -d '\r\n')
[[ "$linux_sha" == "$source_sha" && "$windows_sha" == "$source_sha" ]]
version=$(tar -xOf dist/phantom-misses-linux-x64.tar.gz phantom-misses-linux-x64/PHANTOM_VERSION | tr -d '\r\n')
windows_version=$(unzip -p dist/phantom-misses-win-x64.zip phantom-misses-win-x64/PHANTOM_VERSION | tr -d '\r\n')
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ && "$version" == "$windows_version" ]]
tag="phantom-v$version"

if gh release view "$tag" --json databaseId,isDraft,targetCommitish,url > "$work/release.json" 2>/dev/null; then
    if [[ "$(jq -r .isDraft "$work/release.json")" != true ]]; then
        echo "Release $tag is already published and is left unchanged. Bump PHANTOM_VERSION for an update." | tee -a "$GITHUB_STEP_SUMMARY"
        exit 0
    fi
    [[ "$(jq -r .targetCommitish "$work/release.json")" == "$source_sha" ]] || {
        echo 'This draft belongs to a different source commit. Recover its original build or use a new version.'
        exit 1
    }
else
    # Get the notes from the packaged commit, including when recovering an older build.
    gh api "repos/$GH_REPO/contents/PHANTOM_RELEASE_NOTES.md?ref=$source_sha" --jq .content | base64 --decode > "$work/notes.md"
    printf '\n## Build provenance\n\nSource commit: `%s`\n\n[Build and test run](https://github.com/%s/actions/runs/%s)\n' "$source_sha" "$GH_REPO" "$source_run" >> "$work/notes.md"
    gh release create "$tag" --draft --target "$source_sha" --title "Phantom Misses $version" --notes-file "$work/notes.md"
    gh release view "$tag" --json databaseId,isDraft,targetCommitish,url > "$work/release.json"
fi
release_id=$(jq -r .databaseId "$work/release.json")
[[ "$release_id" =~ ^[0-9]+$ ]]
upload_url=$(gh api "repos/$GH_REPO/releases/$release_id" --jq .upload_url)
upload_url="${upload_url%%\{*}"
[[ "$upload_url" == "https://uploads.github.com/repos/$GH_REPO/releases/$release_id/assets" ]]

for file in dist/phantom-misses-linux-x64.tar.gz dist/phantom-misses-linux-x64.tar.gz.sha256 dist/phantom-misses-win-x64.zip dist/phantom-misses-win-x64.zip.sha256; do
    name=$(basename "$file")
    size=$(stat -c %s "$file")
    digest="sha256:$(sha256sum "$file" | cut -d ' ' -f 1)"
    for attempt in 1 2 3; do
        gh api "repos/$GH_REPO/releases/$release_id/assets?per_page=100" > "$work/assets.json"
        asset=$(jq -c --arg name "$name" '.[] | select(.name == $name)' "$work/assets.json")
        if [[ -n "$asset" ]]; then
            if [[ "$(jq -r .state <<< "$asset")" == uploaded ]]; then
                # Never replace a completed upload. A different digest is an error, not permission to overwrite.
                jq -e --arg digest "$digest" --argjson size "$size" '.digest == $digest and .size == $size' <<< "$asset" >/dev/null
                echo "Verified uploaded asset: $name"
                break
            fi
            [[ "$(gh api "repos/$GH_REPO/releases/$release_id" --jq .draft)" == true ]]
            asset_id=$(jq -r .id <<< "$asset")
            gh api --method DELETE "repos/$GH_REPO/releases/assets/$asset_id"
        fi
        [[ "$(gh api "repos/$GH_REPO/releases/$release_id" --jq .draft)" == true ]]
        echo "Uploading $name (attempt $attempt)..."
        # Sequential HTTP/1.1 uploads avoid the stalled concurrent CLI transfer path.
        if curl --http1.1 --fail-with-body --show-error --silent --connect-timeout 30 --max-time 600 \
            --speed-limit 65536 --speed-time 60 \
            --header "Authorization: Bearer $GH_TOKEN" \
            --header 'Accept: application/vnd.github+json' \
            --header 'Content-Type: application/octet-stream' \
            --data-binary "@$file" "$upload_url?name=$name" \
            --output "$work/uploaded.json" \
            --write-out 'HTTP %{http_code}, %{size_upload} bytes, %{time_total} seconds\n'; then
            jq -e --arg digest "$digest" --argjson size "$size" '.state == "uploaded" and .digest == $digest and .size == $size' "$work/uploaded.json" >/dev/null
            echo "Verified uploaded asset: $name"
            break
        fi
        [[ "$attempt" != 3 ]] || { echo "Upload failed: $name"; exit 1; }
        sleep 5
    done
done

gh api "repos/$GH_REPO/releases/$release_id/assets?per_page=100" > "$work/assets.json"
jq -e 'length == 4 and all(.[]; .state == "uploaded")' "$work/assets.json" >/dev/null
gh api --method PATCH "repos/$GH_REPO/releases/$release_id" -F draft=false -f make_latest=true --jq .html_url | tee -a "$GITHUB_STEP_SUMMARY"
