dev: 
  aspire run -d

codegen:
  kiota generate --output ./HackOMania.WebApp/app/api-client --language TypeScript -d https://hackomania-api.geekshacking.com/openapi/v1.json --co
  kiota info -d https://hackomania-api.geekshacking.com/openapi/v1.json -l TypeScript

codegen-local:
  kiota generate --output ./HackOMania.WebApp/app/api-client --language TypeScript -d http://localhost:5227/openapi/v1.json --co
