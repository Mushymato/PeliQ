# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.2] - 2025-08-05

- Traction mushymato.PeliQ_Divorce for FL

## [1.1.1] - 2025-05-02

- Hotfix, restrict where nested query can apply.

## [1.1.0] - 2025-05-01

- GSQ mushymato.PeliQ_ANY_N: meta query that checks at least X GSQs are true.
- GSQ mushymato.PeliQ_AND, mushymato.PeliQ_OR, mushymato.PeliQ_XOR: logical GSQ for 2 sub GSQs
- Traction mushymato.PeliQ_Divorce
- Expanded StoredQuery with some machine fields for custom flavored items, can't do EMC type things though.

## [1.0.0] - 2025-01-25

### Added

- ActionSalable: placeholder item for using actions in shops
- NestedQuery: do item query using result of another item query
- StoredQuery: item queries custom asset for item queries/action/map action/mail
