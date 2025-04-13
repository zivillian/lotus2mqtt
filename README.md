# Ziel

Hier entsteht eine Anwendung um den aktuellen Status eines Lotus Eletre per MQTT zu veröffentlichen (und später vielleicht über MQTT Befehle an das Auto zu senden).

# Ich will mitmachen...

Sehr gern! Ich hab aktuell auch noch keinen Plan, was die nächsten Schritte sind. Daher ist es am besten wenn du [einen Issue aufmachst](https://github.com/zivillian/lotus2mqtt/issues/new) und sagst was du vorhast, kannst, willst, brauchst...

# Status

Es gibt eine Kommandozeilenanwendung die die aktuellen Werte auslesen und per MQTT veröffentlichen kann.

Im ersten Schritt muss die Konfigurationsdatei mit `lotus2mqtt configure` erstellt werden. Dafür am besten einen zusätzlichen Account anlegen und das Auto für diesen Account freigeben. Anschließend kann die Anwendung mit `lotus2mqtt run` oder einfach `lotus2mqtt` gestartet werden. Damit sollten die aktuellen Werte im MQTT zu sehen sein.

Die Werte (SOC, Range, Odometer und LimitSoc) können in [evcc](https://github.com/evcc-io/evcc/) mit der folgenden Konfiguration eingebunden werden:

```yaml
vehicles:
- name: lotus
  type: custom
  title: Lotus Eletre
  capacity: 112
  phases: 3
  soc:
    source: mqtt
    topic: lotus/<vin>/additionalVehicleStatus/electricVehicleStatus/chargeLevel
    timeout: 1m
  range:
    source: mqtt
    topic: lotus/<vin>/additionalVehicleStatus/electricVehicleStatus/distanceToEmptyOnBatteryOnly
    timeout: 1m
  odometer:
    source: mqtt
    topic: lotus/<vin>/additionalVehicleStatus/maintenanceStatus/odometer
    timeout: 1m
  limitsoc:
    source: mqtt
    topic: lotus/<vin>/soc/soc
    timeout: 1m
    scale: 0.1
```

TODO:
- [ ] Token Refresh
- [ ] `latest` tag auf docker

## Docker

Es gibt einen Docker Container. Die config muss vorher mit `lotus2mqtt configure` erstellt werden:

```bash
docker run -d --restart=unless-stopped -v ./lotus2mqtt.yml:/config/lotus2mqtt.yml zivillian/lotus2mqtt:main
```
