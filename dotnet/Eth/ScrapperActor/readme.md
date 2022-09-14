$Env:PORT=5002

dapr run --app-port $Env:Port --app-id scrapper-actor --components-path ../../../components --dapr-http-port 3502 -- dotnet watch run