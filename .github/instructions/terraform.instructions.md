---
description: 'Terraform Conventions and Guidelines'
applyTo: '**/*.tf'
---

# Terraform Conventions (Azure-first)

## General Instructions

- Use Terraform to provision and manage infrastructure.
- Use version control for your Terraform configurations.

## Security

- Use the latest stable Terraform and provider versions; update regularly.
- Do not commit credentials, client secrets, or state files.
  - Prefer Azure Key Vault to store secrets; reference them via data sources or pipeline secrets.
  - Use Managed Identity (MSI) where possible instead of client secrets.
  - Mark sensitive variables and outputs with `sensitive = true`.
- Follow least-privilege RBAC for Azure resources (e.g., AcrPull on ACR for AKS).
- Use virtual networks, subnets, and NSGs to control network access.
- Enable encryption at rest and in transit (TLS) for services that support it.
- Scan Terraform with `tfsec`/`checkov` and container images with `trivy`.

## Modularity

- Use separate projects for each major component of the infrastructure; this:
  - Reduces complexity
  - Makes it easier to manage and maintain configurations
  - Speeds up `plan` and `apply` operations
  - Allows for independent development and deployment of components
  - Reduces the risk of accidental changes to unrelated resources
- Use modules to avoid duplication of configurations.
  - Use modules to encapsulate related resources and configurations.
  - Use modules to simplify complex configurations and improve readability.
  - Avoid circular dependencies between modules.
  - Avoid unnecessary layers of abstraction; use modules only when they add value.
    - Avoid using modules for single resources; only use them for groups of related resources.
    - Avoid excessive nesting of modules; keep the module hierarchy shallow.
- Use `output` blocks to expose important information about your infrastructure.
  - Use outputs to provide information that is useful for other modules or for users of the configuration.
  - Avoid exposing sensitive information in outputs; mark outputs as `sensitive = true` if they contain sensitive data.

## Maintainability

- Prioritize readability, clarity, and maintainability.
- Use comments to explain complex configurations and why certain design decisions were made.
- Write concise, efficient, and idiomatic configs that are easy to understand.
- Avoid using hard-coded values; use variables for configuration instead.
  - Set default values for variables, where appropriate.
- Use data sources to retrieve information about existing resources instead of requiring manual configuration.
  - This reduces the risk of errors, ensures that configurations are always up-to-date, and allows configurations to adapt to different environments.
  - Avoid using data sources for resources that are created within the same configuration; use outputs instead.
  - Avoid, or remove, unnecessary data sources; they slow down `plan` and `apply` operations.
- Use `locals` for values that are used multiple times to ensure consistency.

## Style and Formatting

- Follow Terraform best practices for resource naming and organization.
  - Use descriptive names for resources, variables, and outputs.
  - Use consistent naming conventions across all configurations.
- Follow the **Terraform Style Guide** for formatting.
  - Use consistent indentation (2 spaces for each level).
- Group related resources together in the same file.
  - Use a consistent naming convention for resource groups (e.g., `providers.tf`, `variables.tf`, `network.tf`, `ecs.tf`, `mariadb.tf`).
- Place `depends_on` blocks at the very beginning of resource definitions to make dependency relationships clear.
  - Use `depends_on` only when necessary to avoid circular dependencies.
- Place `for_each` and `count` blocks at the beginning of resource definitions to clarify the resource's instantiation logic.
  - Use `for_each` for collections and `count` for numeric iterations.
  - Place them after `depends_on` blocks, if they are present.
- Place `lifecycle` blocks at the end of resource definitions.
- Alphabetize providers, variables, data sources, resources, and outputs within each file for easier navigation.
- Group related attributes together within blocks.
  - Place required attributes before optional ones, and comment each section accordingly.
  - Separate attribute sections with blank lines to improve readability.
  - Alphabetize attributes within each section for easier navigation.
- Use blank lines to separate logical sections of your configurations.
- Use `terraform fmt` to format your configurations automatically.
- Use `terraform validate` to check for syntax errors and ensure configurations are valid.
- Use `tflint` to check for style violations and ensure configurations follow best practices.
  - Run `tflint` regularly to catch style issues early in the development process.

## Documentation

- Always include `description` and `type` attributes for variables and outputs.
  - Use clear and concise descriptions to explain the purpose of each variable and output.
  - Use appropriate types for variables (e.g., `string`, `number`, `bool`, `list`, `map`).
- Document your Terraform configurations using comments, where appropriate.
  - Use comments to explain the purpose of resources and variables.
  - Use comments to explain complex configurations or decisions.
  - Avoid redundant comments; comments should add value and clarity.
- Include a `README.md` file in each project to provide an overview of the project and its structure.
  - Include instructions for setting up and using the configurations.
- Use `terraform-docs` to generate documentation for your configurations automatically.

## Testing

## Naming

- Follow Azure resource naming guidelines and abbreviations.
  - Cloud Adoption Framework: https://learn.microsoft.com/en-us/azure/cloud-adoption-framework/ready/azure-best-practices/resource-naming
  - Apply environment name as a suffix (e.g., `rg-wi-sample-dev`, `aks-wi-sample-dev`, `acrwisampledev`).
  - Keep names deterministic and lowercase; respect each resource’s constraints.

- Write tests to validate the functionality of your Terraform configurations.
  - Use the `.tftest.hcl` extension for test files.
  - Write tests to cover both positive and negative scenarios.
  - Ensure tests are idempotent and can be run multiple times without side effects.
