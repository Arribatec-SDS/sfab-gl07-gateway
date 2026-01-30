# GitHub Actions Workflow

This workflow automatically builds and publishes Docker images for your Nexus app to GitHub Container Registry (ghcr.io).

## Setup Requirements

To use this workflow, you need to configure one secret in your GitHub repository:

1. Go to your repository on GitHub
2. Navigate to **Settings → Secrets and variables → Actions**
3. Create the following secret:
   - **`GH_PACKAGES_TOKEN`**: A Personal Access Token with the following scopes:
     - `read:packages` - to read npm packages from GitHub Packages
     - `write:packages` - to push Docker images to GitHub Container Registry

### Creating a Personal Access Token

1. Go to GitHub **Settings → Developer settings → Personal access tokens → Tokens (classic)**
2. Click **Generate new token (classic)**
3. Give it a descriptive name (e.g., "Nexus App CI/CD")
4. Select scopes:
   - ✅ `read:packages`
   - ✅ `write:packages`
   - ✅ `repo` (if your repository is private)
5. Click **Generate token**
6. Copy the token and add it as a repository secret named `GH_PACKAGES_TOKEN`

## What the Workflow Does

### Triggers
- **Push to main/master**: Builds and publishes the image with version tag and `latest`
- **Pull requests**: Builds the image but doesn't push it (validation only)
- **Manual**: Can be triggered manually via GitHub Actions UI

### Build Process
1. Checks out the code
2. Sets up Node.js and npm with GitHub Packages authentication
3. Installs and builds the frontend
4. Builds the Docker image with multi-stage build
5. Tags the image with:
   - Date-based version: `YYYYMMDD.N` (e.g., `20240127.1`)
   - `latest` (only on main/master branch)
6. Pushes the image to `ghcr.io/<owner>/<repo>`

### Version Tagging

The workflow uses a date-based versioning scheme:
- Format: `YYYYMMDD.N`
- Example: `20240127.1` (first build on Jan 27, 2024)
- Increments `N` for each build on the same day
- Resets to 1 at midnight UTC

## Using the Published Image

After a successful build, your image will be available at:

```
ghcr.io/<your-github-username>/<your-repo-name>:latest
ghcr.io/<your-github-username>/<your-repo-name>:20240127.1
```

### Pull the Image

To pull and run your published image:

```bash
# Login to GitHub Container Registry
echo $GITHUB_TOKEN | docker login ghcr.io -u <your-username> --password-stdin

# Pull the image
docker pull ghcr.io/<your-username>/<your-repo>:latest

# Run it
docker run -p 80:80 ghcr.io/<your-username>/<your-repo>:latest
```

### Update docker-compose.yml

To use the published image in docker-compose:

```yaml
services:
  app:
    image: ghcr.io/<your-username>/<your-repo>:latest
    # ... rest of your config
```

## Troubleshooting

### Build fails with "npm install" errors
- Ensure `GH_PACKAGES_TOKEN` secret is set correctly
- Verify the token has `read:packages` scope
- Check that the token hasn't expired

### Image push fails
- Ensure `GH_PACKAGES_TOKEN` has `write:packages` scope
- Verify GitHub Container Registry is enabled for your account

### Permission denied
- Make sure the workflow has `packages: write` permission
- Check repository settings allow GitHub Actions to write packages

## Customization

You can customize the workflow by editing [.github/workflows/docker-publish.yml](.github/workflows/docker-publish.yml):

- **Change triggers**: Modify the `on:` section
- **Add build arguments**: Add to `build-args` in the build step
- **Change versioning**: Modify the "Generate date-based version tag" step
- **Add deployment**: Create additional jobs after `build-and-push`

## Advanced: Automated Deployment

The exchange-rates app includes an optional deployment step. You can add similar deployment automation by creating a `deploy-swarm.yml` workflow and uncommenting the deploy job in this workflow.
