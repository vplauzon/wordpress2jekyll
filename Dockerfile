#	Use a Microsoft image with .NET core runtime (https://hub.docker.com/r/microsoft/dotnet/tags/)
FROM mcr.microsoft.com/dotnet/core/runtime:3.0 AS final

#	Set the working directory to /work
WORKDIR /work

#	Copy package
COPY app .

#	Define environment variables
ENV INPUT_PATH "input-not-set"
ENV OUTPUT_PATH "output-not-set"
ENV DO_IMAGES "true"

#	Run console app
CMD ["sh", "-c", "dotnet wordpress2jekyll.dll --do-images=${DO_IMAGES} ${INPUT_PATH} ${OUTPUT_PATH}"]