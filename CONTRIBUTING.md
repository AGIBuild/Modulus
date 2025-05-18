# Contributing to Modulus

Thank you for your interest in contributing to Modulus! This guide will help you get started with contributing to the project.

## Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/your-username/modulus.git`
3. Create a new branch: `git checkout -b feature/your-feature-name`
4. Make your changes
5. Run tests: `nuke test`
6. Commit your changes: `git commit -m "Add feature"`
7. Push to your fork: `git push origin feature/your-feature-name`
8. Create a pull request

## Using AI Context with GitHub Copilot

Modulus provides a built-in system to bootstrap AI context for tools like GitHub Copilot, making it easier for you to understand the project and get AI assistance that aligns with project conventions.

### Using the StartAI Command

Before starting development with AI assistance, run:

```powershell
nuke StartAI
```

This command will output comprehensive project context that you can paste into GitHub Copilot Chat to bootstrap its understanding of Modulus.

For role-specific context, use the `--role` parameter:

```powershell
# For backend developers
nuke StartAI --role Backend

# For frontend developers
nuke StartAI --role Frontend  

# For plugin developers
nuke StartAI --role Plugin

# For documentation contributors
nuke StartAI --role Docs
```

### Quick Reference Commands for Copilot Chat

After providing context to Copilot, you can use the following commands in Copilot Chat:

- `/sync` - Refresh project context
- `/roadmap` - View project roadmap
- `/why <file>` - Get explanation about specific file's purpose

## Documentation Standards

- All user-facing documentation should be in both English and Chinese
- All story documents must have bilingual versions (in `docs/en-US/stories/` and `docs/zh-CN/stories/`)
- Follow the story naming convention: `S-XXXX-Title.md`
- Include priority and status tags in story documents

## Code Style Guidelines

- Use PascalCase for class names and public members
- Use camelCase for local variables and parameters  
- Prefix private fields with underscore (`_privateField`)
- Include XML documentation for public APIs
- Write unit tests for all new functionality

## Building and Running

- Use the Nuke build system: `nuke --help` for available targets
- Run the application: `nuke run`
- Build all components: `nuke build`
- Run tests: `nuke test`
- Pack plugins: `nuke plugin`

## Pull Request Process

1. Ensure your code follows the project's style guidelines
2. Update documentation as needed
3. Include tests for new functionality  
4. Make sure all tests pass before submitting
5. Link any related issues in your PR description
6. Wait for review from project maintainers

## Need Help?

If you have any questions, feel free to open an issue or join our community channels.
