FROM mono:latest

COPY . /build

RUN nuget restore /build/EnergySmartBridge.sln
RUN msbuild /build/EnergySmartBridge.sln /t:Build /p:Configuration=Release

RUN mv /build/EnergySmartBridge/bin/Release /app
RUN rm -rf /build

EXPOSE 8001/tcp

VOLUME /config

WORKDIR /app

CMD [ "mono",  "EnergySmartBridge.exe", "-i", "-c", "/config/EnergySmartBridge.ini" ]