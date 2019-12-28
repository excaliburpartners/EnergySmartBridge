FROM mono:latest AS build

WORKDIR /build
COPY . .
RUN nuget restore /build/EnergySmartBridge.sln
RUN msbuild /build/EnergySmartBridge.sln /t:Build /p:Configuration=Release
RUN mv /build/EnergySmartBridge/bin/Release /app

FROM mono:latest AS runtime

COPY --from=build /app/EnergySmartBridge.ini /config/EnergySmartBridge.ini

EXPOSE 8001/tcp
VOLUME /config
WORKDIR /app
COPY --from=build /app .
CMD [ "mono",  "EnergySmartBridge.exe", "-i", "-c", "/config/EnergySmartBridge.ini", "-e" ]