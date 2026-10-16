# Overview and features
This project is an MCP tool and a REST API with following features:
- Semantically search documentation
- Serve documents
- Ingest, chunk and store documents

# Technologies
- .NET 10
- Entity Framework
- PGvector
- Ollama with embeddinggemma model

# Project structure
- /App folder contains the main .NET application project
- /Common contains library project for code, that is used by all projects inside this solution.
- /Tests contains test project that uses MSTest.Sdk.

# Devcontainer
This project is primarily developed in a development container. Its images and docker compose contain all tools and services required for development. Devcontainer uses Github copilot sandbox features.