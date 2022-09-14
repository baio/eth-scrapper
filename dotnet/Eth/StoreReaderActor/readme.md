$Env:PORT=5004

dapr run --app-port $Env:Port --app-id store-reader --components-path ../../../components --dapr-http-port 3504 -- dotnet watch run
