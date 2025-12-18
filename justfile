dev: 
  aspire run -d

codegen:
  kiota generate --output ./HackOMania.WebApp/app/api-client --language TypeScript -d https://localhost:7256/openapi/v1.json --co
  kiota info -d https://localhost:7256/openapi/v1.json -l TypeScript
