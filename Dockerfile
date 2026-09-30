# syntax=docker/dockerfile:1
# The Stack86 demo, built from the repository root:
#
#     docker build --secret id=github_packages_user,env=GITHUB_PACKAGES_USER \
#         --secret id=github_packages_token,env=GITHUB_PACKAGES_TOKEN -t stack86-demo .
#
# The context is the root because Directory.Build.props and stylecop.json decide the rules the
# code is compiled under.

# The Angular client. Its vite config writes to ../wwwroot, so the output lands at /wwwroot.
# Node 24 for the npm 11 the lock file was written with; npm 10 reads it as out of date.
FROM node:24 AS client
WORKDIR /client

# The manifests first, so a source change does not reinstall the packages.
COPY src/Stack86.Web/ClientApp/package.json src/Stack86.Web/ClientApp/package-lock.json ./
RUN npm ci

COPY src/Stack86.Web/ClientApp/ ./
RUN npm run build

# The typescript package the transpiler loads through NODE_PATH at runtime.
FROM node:22 AS transpiler
WORKDIR /transpiler
RUN npm install --no-save typescript@6.0.3

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# The project files on their own layer, so a source change does not re-restore packages.
COPY global.json NuGet.config ./
COPY src/Directory.Build.props src/stylecop.json src/
COPY src/Stack86.Common/Stack86.Common.csproj src/Stack86.Common/
COPY src/Stack86.Logic/Stack86.Logic.csproj src/Stack86.Logic/
COPY src/Stack86.Api/Stack86.Api.csproj src/Stack86.Api/
COPY src/Stack86.Web/Stack86.Web.csproj src/Stack86.Web/
# The shared Common.* packages come from the private GitHub Packages feed NuGet.config names.
# Its token is a build secret, so it never lands in a layer or the image history.
RUN --mount=type=secret,id=github_packages_user,env=GITHUB_PACKAGES_USER \
    --mount=type=secret,id=github_packages_token,env=GITHUB_PACKAGES_TOKEN \
    dotnet restore src/Stack86.Web/Stack86.Web.csproj

COPY src/ src/

# The csproj publishes wwwroot as content, so the client has to be in place before it runs.
COPY --from=client /wwwroot src/Stack86.Web/wwwroot
RUN dotnet publish src/Stack86.Web/Stack86.Web.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# tcc validates C, node validates JavaScript and runs the TypeScript transpiler.
RUN apt-get update \
    && apt-get install -y --no-install-recommends tcc \
    && rm -rf /var/lib/apt/lists/*

# A known node, rather than the distribution's older one. The bookworm build is deliberate: it
# links against an older glibc than this image carries, and a newer one would not run here.
COPY --from=node:22-bookworm-slim /usr/local/bin/node /usr/local/bin/node

COPY --from=transpiler /transpiler/node_modules ./node_modules
COPY --from=build /app .

# Compiles run in temporary files under /tmp, so nothing here writes to its own directory.
USER $APP_UID

# The platform terminates TLS in front of this, so the container serves plain HTTP on 8080.
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "Stack86.Web.dll"]
