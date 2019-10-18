#	Use a Microsoft image with .NET core runtime (https://hub.docker.com/r/microsoft/dotnet/tags/)
FROM microsoft/dotnet:2.1-aspnetcore-runtime AS final

#	Set the working directory to /work
WORKDIR /work

#	Copy package
COPY wordpress2jekyll/app .

#	Define environment variables
ENV TODO ""

#	Run console app
CMD ["dotnet", "wordpress2jekyll.dll"]