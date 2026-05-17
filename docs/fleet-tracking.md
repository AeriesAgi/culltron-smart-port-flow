# Fleet tracking and GPS/check-in flow

Smart Port uses app-based check-ins, not background surveillance.

## Data captured

Truck reference, driver, fleet owner, job/booking reference, latitude, longitude, last check-in timestamp, queue/stage/status, next instruction, delay risk and idling/CO₂ estimate.

## Flow

1. Driver opens `/driver-app` or Android app.
2. Driver taps GPS/manual Check In.
3. Backend records `LocationCheckIn` in demo state.
4. `/fleet/tracker`, truck detail and audit timelines update.
5. Copilot can explain route/stage risk and recommended next action.

## Map rendering

The current demo uses a premium dark map-style panel from check-in data. A production pilot can connect Google Maps/GIS/telematics without changing the operational workflow.
