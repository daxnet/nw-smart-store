FROM nginx:1.29.0-alpine AS base
WORKDIR /usr/share/nginx/html
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS publish
WORKDIR /src
COPY . .
RUN dotnet publish "Nss.WebApp.csproj" -c Release -o /app/publish

FROM base as final
COPY --from=publish /src/nginx.conf /etc/nginx/nginx.conf
COPY --from=publish /app/publish/wwwroot .
#COPY subst_env.sh /usr/share/nginx/html/subst_env.sh
#RUN chmod +x subst_env.sh
#CMD /usr/share/nginx/html/subst_env.sh && nginx -g "daemon off;"
CMD ["nginx", "-g", "daemon off;"]
