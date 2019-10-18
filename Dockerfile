#	Use a Microsoft image with .NET core runtime (https://hub.docker.com/r/microsoft/dotnet/tags/)
FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS final

#	Set the working directory to /work
WORKDIR /work

#	Copy package
COPY app .

#	Define environment variables
ENV INPUT_PATH ""
ENV OUTPUT_PATH ""

#	Run console app
CMD ["dotnet", "wordpress2jekyll.dll", "${INPUT_PATH}", "${OUTPUT_PATH}"]