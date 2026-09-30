#!/usr/bin/env bash
# Attaches the files fetch-release-sources.sh gathered to the GitHub Release of <tag>,
# creating it as a draft when it does not exist yet.
#
# Idempotent, and never swaps the sources of one build for another's: SHA256SUMS goes up
# last and marks a complete upload. When the release already has one:
#   - identical: a re-run of the same build; only missing assets are uploaded;
#   - different: this run bundled other versions than the run that attached them (a full
#     re-run of the tag after MSYS2 or Homebrew moved on), so it fails rather than replace
#     the sources of what may already be on nuget.org.
# Without SHA256SUMS, an earlier upload stopped part way and its assets are replaced.
#
# usage: attach-release-sources.sh <tag> <dir>
# Needs GH_TOKEN (contents: write) and GH_REPO (owner/name) in the environment.
set -euo pipefail

tag=${1:?usage: attach-release-sources.sh <tag> <dir>}
dir=${2:?usage: attach-release-sources.sh <tag> <dir>}
: "${GH_TOKEN:?}" "${GH_REPO:?}"

fail() { echo "::error::$*" >&2; exit 1; }

[ -s "$dir/SHA256SUMS" ] || fail "no SHA256SUMS in $dir"
assets=()
for f in "$dir"/*; do
    [ "${f##*/}" = SHA256SUMS ] || assets+=("$f")
done
[ "${#assets[@]}" -gt 0 ] || fail "nothing to attach in $dir"

tmp=$(mktemp -d)
trap 'rm -rf "$tmp"' EXIT

# gh release view also finds a draft by its pending tag name. Only "release not found" means
# there is none: any other failure (auth, network, rate limit) must not create a second draft.
if existing=$(gh release view "$tag" --json assets --jq '.assets[].name' 2> "$tmp/err"); then
    echo "Release $tag exists"
elif grep -q 'release not found' "$tmp/err"; then
    notes="The native libraries in this NuGet package are built from the sources attached below: \
native-sources-<rid>.tar holds, per platform, each bundled library's source with the patches \
and build recipe it was built with, and its aravis-native-versions.txt; aravis-*.tar.gz is \
the Aravis source. SHA256SUMS lists their checksums. See THIRD-PARTY-NOTICES.md."
    gh release create "$tag" --draft --verify-tag --title "$tag" --notes "$notes"
    existing=
else
    cat "$tmp/err" >&2
    fail "cannot look up the release $tag"
fi

if grep -qx SHA256SUMS <<<"$existing"; then
    gh release download "$tag" --pattern SHA256SUMS --dir "$tmp"
    if ! cmp -s "$tmp/SHA256SUMS" "$dir/SHA256SUMS"; then
        diff "$tmp/SHA256SUMS" "$dir/SHA256SUMS" >&2 || true
        fail "release $tag already carries the sources of a different build. If that build never reached nuget.org, delete its assets (or the draft) and re-run; otherwise its sources are the right ones and this build must not be published."
    fi
    missing=()
    for f in "${assets[@]}"; do
        grep -qxF "${f##*/}" <<<"$existing" || missing+=("$f")
    done
    if [ "${#missing[@]}" -gt 0 ]; then
        gh release upload "$tag" "${missing[@]}"
    fi
    echo "Release $tag already has these sources"
else
    gh release upload "$tag" "${assets[@]}" --clobber
    gh release upload "$tag" "$dir/SHA256SUMS" --clobber
fi

gh release view "$tag" --json assets --jq '.assets[] | "\(.name)\t\(.size)"'
