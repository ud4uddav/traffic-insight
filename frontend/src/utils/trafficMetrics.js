export function averageReportedSpeed(stations) {
  const speeds = stations
    .map((station) => station.averageSpeedKmh)
    .filter(Number.isFinite);

  if (speeds.length === 0) {
    return null;
  }

  return Math.round(speeds.reduce((total, speed) => total + speed, 0) / speeds.length);
}
