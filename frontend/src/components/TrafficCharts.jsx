import {
  Bar,
  BarChart,
  CartesianGrid,
  ResponsiveContainer,
  Tooltip,
  XAxis,
  YAxis,
} from 'recharts';

function TrafficCharts({ data }) {
  const vehicleData = data
    .filter((item) => Number.isFinite(item.vehiclesPerHour))
    .map((item) => ({
      station: item.stationName?.replaceAll('_', ' ') || `Station ${item.stationId}`,
      vehicles: item.vehiclesPerHour,
    }));

  const speedData = data
    .filter((item) => Number.isFinite(item.averageSpeedKmh))
    .map((item) => ({
      station: item.stationName?.replaceAll('_', ' ') || `Station ${item.stationId}`,
      speed: item.averageSpeedKmh,
    }));

  return (
    <section className="charts-section" aria-labelledby="analytics-heading">
      <div className="section-heading">
        <div>
          <span className="eyebrow">LIVE MEASUREMENTS</span>
          <h2 id="analytics-heading">Traffic analytics</h2>
        </div>
        <p>Charts include only values reported by Digitraffic.</p>
      </div>

      <div className="charts-grid">
        <article className="chart-card">
          <h3>Vehicles per hour by station</h3>
          {vehicleData.length > 0 ? (
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={vehicleData} margin={{ top: 8, right: 8, left: 0, bottom: 38 }}>
                <CartesianGrid stroke="#e8edf3" vertical={false} />
                <XAxis dataKey="station" angle={-25} textAnchor="end" interval="preserveStartEnd" />
                <YAxis />
                <Tooltip />
                <Bar dataKey="vehicles" name="Vehicles / hour" fill="#0f766e" radius={[5, 5, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : <p className="empty-chart">No vehicle readings are available right now.</p>}
        </article>

        <article className="chart-card">
          <h3>Average speed by station</h3>
          {speedData.length > 0 ? (
            <ResponsiveContainer width="100%" height={300}>
              <BarChart data={speedData} margin={{ top: 8, right: 8, left: 0, bottom: 38 }}>
                <CartesianGrid stroke="#e8edf3" vertical={false} />
                <XAxis dataKey="station" angle={-25} textAnchor="end" interval="preserveStartEnd" />
                <YAxis unit=" km/h" />
                <Tooltip formatter={(value) => [`${value} km/h`, 'Average speed']} />
                <Bar dataKey="speed" name="Average speed" fill="#2563eb" radius={[5, 5, 0, 0]} />
              </BarChart>
            </ResponsiveContainer>
          ) : <p className="empty-chart">Speed data is unavailable for the current stations.</p>}
        </article>
      </div>
    </section>
  );
}

export default TrafficCharts;
