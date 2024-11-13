---
Order: 40
Title: Version Incrementing
Description: Details on how GitVersion performs version increments
RedirectFrom:
- docs/more-info/incrementing-per-commit
- docs/more-info/version-increments
---

Because GitVersion works with several workflows, the way it does its version
incrementing may work perfectly for you, or it may cause you issues. This page
is split up into two sections, first is all about understanding the approach
GitVersion uses by default, and the second is how you can manually increment the
version.

## Approach

Semantic Versioning is all about _releases_, not commits or builds. This means
that the version only increases after you release, this directly conflicts with
the concept of published CI builds. When you release the next version of your
library/app/website/whatever you should only increment major/minor or patch then
reset all lower parts to 0, for instance given `1.0.0`, the next release should
be either `2.0.0`, `1.1.0` or `1.0.1`.

Because of this, GitVersion works out what the next SemVer of your app is on
each commit. When you are ready to release, you simply deploy the latest built
version and tag the commit it was created from. This practice is called
[continuous delivery][continuous-delivery]. GitVersion will increment the
_metadata_ for each commit so you can tell them apart. For example `1.0.0+5`
followed by `1.0.0+6`. It is important to note that build metadata _is not part
of the semantic version; it is just metadata!_.

All this effectively means that GitVersion will produce the same version NuGet
package each commit until you tag a release.

This causes problems for people as NuGet and other package managers do not
support multiple packages with the same version where only the metadata is
different. There are a few ways to handle this problem depending on what your
requirements are:

### 1. GitFlow

If you are using GitFlow then builds off the `develop` branch will actually
_increment on every commit_. This is known in GitVersion as _continuous
deployment mode_. By default `develop` builds are tagged with the `alpha`
pre-release tag. This is so they are sorted higher than release branches.

If you need to consume packages built from `develop`, we recommend publishing
these packages to a separate NuGet feed as an alpha channel. That way you can
publish beta/release candidate builds and only people who opt into the alpha
feed will see the alpha packages.

### 2. Octopus deploy

See [Octopus deploy](/docs/reference/build-servers/octopus-deploy)

## Manually incrementing the version

With v3 there are multiple approaches. Read about these below.

### Commit messages

Adding `+semver: breaking` or `+semver: major` will cause the major version to
be increased, `+semver: feature` or `+semver: minor` will bump minor and
`+semver: patch` or `+semver: fix` will bump the patch.

#### Configuration

The feature is enabled by default but can be disabled via configuration, the
regex we use can be changed:

```yaml
major-version-bump-message: '\+semver:\s?(breaking|major)'
minor-version-bump-message: '\+semver:\s?(feature|minor)'
patch-version-bump-message: '\+semver:\s?(fix|patch)'
commit-message-incrementing: Enabled
```

The options for `commit-message-incrementing` are `Enabled`, `MergeMessageOnly`
and `Disabled`

If the incrementing `mode` is set to `MergeMessageOnly` you can add this
information when merging a pull request. This prevents commits within a PR to
bump the version number.

#### Conventional commit messages

If you want to use the [Conventional Commits][conventional-commits] standard,
you can leverage this feature as follows:

```yaml
mode: MainLine # Only add this if you want every version to be created automatically on your main branch.
major-version-bump-message: "^(build|chore|ci|docs|feat|fix|perf|refactor|revert|style|test)(\\([\\w\\s-,/\\\\]*\\))?(!:|:.*\\n\\n((.+\\n)+\\n)?BREAKING CHANGE:\\s.+)"
minor-version-bump-message: "^(feat)(\\([\\w\\s-,/\\\\]*\\))?:"
patch-version-bump-message: "^(fix|perf)(\\([\\w\\s-,/\\\\]*\\))?:"
```

This will ensure that your version gets bumped according to the commits you've
created.

If your CI/CD workflow uses semantic-release's commit-analyzer, change
`(fix|perf)` to `(fix|perf|revert)`.
[Why?](https://github.com/semantic-release/commit-analyzer/blob/75c9c87c88772d7ded4ca9614852b42519e41931/lib/default-release-rules.js#L8C1-L8C38)

Alternatively, you can override this rule in the
[configuration](https://github.com/semantic-release/commit-analyzer/tree/master#usage)
of @semantic-release/commit-analyzer. If you intend to write rules with
patterns, note that instead of using Regular Expression,
@semantic-release/commit-analyzer uses
[micromatch's glob implementation](https://github.com/micromatch/micromatch#matching-features).

### GitVersion.yml

The first is by setting the `next-version` property in the GitVersion.yml file.
This property only serves as a base version,

### Branch name

If you create a branch with the version number in the branch name, such as
`release-1.2.0` or `hotfix/1.0.1` then GitVersion will take the version number
from the branch name as a source. However, GitVersion can't use the [branch
name as a version source for _other branches_][faq-branch-name-source].

### Detached HEAD

If HEAD is in detached state tag will be `-no-branch-`.

Example: `0.0.1--no-branch-.1+4`

### Tagging commit

By tagging a commit, GitVersion will use that tag for the version of that
commit, then increment the next commit automatically based on the increment
rules for that branch (some branches bump patch, some minor).

Be aware that tags are local to a repository and will not be transferred when
you perform a default `git push`. Instead, tags can be pushed separately with
their own command. For more information, read the [git documentation on
tagging][git-tagging].

### Incrementing per commit

When using the continuous deployment `mode` (which will increment the SemVer
every commit) all builds _must_ have a pre-release tag, except for builds that
are explicitly tagged as stable.

Then the build metadata (which is the commit count) is promoted to the
pre-release tag. Applying these rules, the above commit-graph would produce:

```log
e137e9 -> 1.0.0+0
a5f6c5 -> 1.0.1-ci.1
adb29a -> 1.0.1-feature-foo.1 (PR #5 Version: `1.0.1-PullRequest.5+2`)
7c2438 -> 1.0.1-feature-foo.2 (PR #5 Version: `1.0.1-PullRequest.5+3`)
5f413b -> 1.0.1-ci.4
d6155b -> 2.0.0-rc.1+4 (Before and after tag)
d53ab6 -> 2.0.0-rc.2 (If there was another commit on the release branch it would be 2.0.0-rc.3)
b5d142 -> 2.0.0-ci.0 (2.0.0 branch was merged, so main is now at 2.0.0)
```

As you can see, the versions now no longer conflict. When you want to create a
stable `2.0.0` release you simply `git tag 2.0.0`, then build the tag, and it
will produce a stable `2.0.0` package. Be aware that
[tags are not transferred with `git push`](#tagging-commit)

For more information/background on why we have come to this conclusion, read
[Xavier Decoster's blog post on the subject][auto-incremented-nuget-package].

[auto-incremented-nuget-package]: https://www.xavierdecoster.com/semantic-versioning-auto-incremented-nuget-package-versions

[continuous-delivery]: /docs/reference/modes/continuous-delivery

[conventional-commits]: https://www.conventionalcommits.org/

[faq-branch-name-source]: /docs/learn/faq#merged-branch-names-as-version-source

[git-tagging]: https://git-scm.com/book/en/v2/Git-Basics-Tagging
