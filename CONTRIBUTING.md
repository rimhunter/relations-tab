# Contributing

Issues and pull requests are welcome on GitHub: https://github.com/rimhunter/relations-tab

- Report a bug or a mod conflict as an issue. Say which RimWorld version (1.6 or 1.7) and which other mods you run, and attach `Player.log` if the game logged an error.
- Pull requests are welcome. Development happens in a separate repository where the build and the in-game test suite run; an accepted pull request is copied there with you kept as the commit author, and it reaches GitHub with the next release.
- The mod builds from this repository with the .NET SDK (`dotnet build Build.proj` produces both the 1.6 and the 1.7 load folder). The in-game scenarios in `TestRunner/` need the shared test runner, which is a private dependency and is not part of this repository. Outside contributors can build the mod and run the unit tests, but can run the scenarios only if they are given access to the runner; the maintainers run them on every accepted change.
- A scenario in `TestRunner/` follows `TestRunner/shared/docs/scenario-standards.md` (five rules: check what a player sees, write expected values out, assert names and cells, make every check able to fail, and put player-facing text through translation keys). Read it before you write or review one.
- Each change is listed with its date in `CHANGELOG.md`, as the licenses require for a modified version. A pull request adds its own line there.
- By contributing you agree that your change is licensed under the same terms as the rest of the mod: the MIT license for code and CC-BY-SA 4.0 for content (see `LICENSE` and `MODIFICATIONS`).
