# EnergySmart Bridge
MQTT bridge for the [Lowe's EnergySmart Water Heater WiFi Controller](https://www.lowes.com/pd/EnergySmart-Electric-Plastic-Water-Heater-Controller/50292493). This controller supports a number of A.O. Smith and Whirlpool water heaters. It might be compatible with the [Kenmore Smart Electric Water Heater Module](https://www.sears.com/kenmore-smart-water-heater-module/p-04258000000P).

## Requirements
- [Docker](https://www.docker.com/)
- DNS for energysmartwaterheater.com needs to be redirected to your docker host IP address
	- [pfSense](https://www.pfsense.org/) - Add Host Override on the DNS Forward or DNS Resolver service
	- [VyOS](https://vyos.io/) - set system static-host-mapping host-name energysmartwaterheater.com inet x.x.x.x
- Port 443 available on your docker host

## How it Works
The controller posts the status of the water heater every five minutes to https://energysmartwaterheater.com. Any queued setting changes are returned in the json response. Due to the controller not accepting a client certificate request and no way to disable in mono, nginx is used as a reverse proxy. Also note the controller only accepts a sha1 certificate.

## Docker
1. Clone git repo and build docker image
	- git clone https://github.com/excaliburpartners/EnergySmartBridge.git
	- cd EnergySmartBridge
	- docker-compose build
2. Configure the MQTT server address and port 
	- mkdir /opt/energysmart-bridge
	- cp EnergySmartBridge/EnergySmartBridge.ini /opt/energysmart-bridge
	- vim /opt/energysmart-bridge/EnergySmartBridge.ini
3. Start docker container
    - docker-compose create
	- docker-compose start
4. Verify connectivity by looking at logs
	- docker-compose logs

## MQTT
This module will also publish discovery topics for Home Assistant to auto configure devices.

```
SUB energysmart/MAC/uppertemp_state 
SUB energysmart/MAC/lowertemp_state 
int Current temperature 

SUB energysmart/MAC/systeminheating_state 
string OFF, ON

SUB energysmart/MAC/hotwatervol_state 
string High, Medium, Low

SUB energysmart/MAC/maxsetpoint_state 
SUB energysmart/MAC/setpoint_state  
PUB energysmart/MAC/setpoint_command    
int Temperature setpoint

SUB energysmart/MAC/mode_state  
PUB energysmart/MAC/mode_command  
string EnergySmart, Standard, Vacation

SUB energysmart/MAC/dryfire_state 
SUB energysmart/MAC/elementfail_state 
SUB energysmart/MAC/tanksensorfail_state 
string None

SUB energysmart/MAC/faultcodes_state
int

SUB energysmart/MAC/signalstrength_state 
int WiFi signal in dBm

SUB energysmart/MAC/updaterate_state 
PUB energysmart/MAC/updaterate_command
int Seconds between updates
```

## Change Log
Version 1.0.0 - 2018-10-25
- Initial release
