# GearBatch

GearBatch helps outdoor-equipment retailers import inventory updates from their suppliers.

A client uploads a CSV file through an authenticated API and receives an import job ID. They can then check the job’s progress, view inventory and download a report of any validation errors.

A background worker processes the file while the API continues handling requests. Accepted jobs are saved in PostgreSQL so unfinished work can be recovered if the application restarts. 

## Planned Scope

- Support one defined CSV format, with each import belonging to a specific supplier.

- Provide API endpoints for uploading files, checking import status, viewing inventory and downloading error reports.

- Process saved jobs using one background worker, retry temporary failures and recover unfinished jobs after a restart.

- Prevent repeated submissions of the same request from creating duplicate jobs.

- Include automated tests, real PostgreSQL tests, CI checks, sample CSV files, a demo and documentation for setting up and reviewing the project.

## Current Status

Initial project setup.

## Repository Structure

```text
gearbatch/                      # Solution/repository folder
├── README.md                   # Project overview and setup
└── gearbatch/                  # Project folder
    ├── src/                    # API and background worker source
    ├── docs/                   # API/database documentation and design decisions
    ├── tests/                  # Automated tests
    ├── results/                # Tests and demos
    ├── db/                     # DB files
    │   └── postgresql/         # Database scripts and assets
    └── data/                   
        └── samples/            # CSV sample files
```