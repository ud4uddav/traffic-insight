import { lazy, Suspense, useEffect, useState } from 'react';
import './App.css';
import Header from './components/Header';
import StatCard from './components/StatCard';

const TrafficCharts = lazy(() => import('./components/TrafficCharts'));
const API_URL = import.meta.env.VITE_API_URL;

function App() {
  const [trafficData, setTrafficData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [lastUpdated, setLastUpdated] = useState(null);

  async function loadTrafficData() {
    setLoading(true);
    setError('');

    try {
      const response = await fetch(`${API_URL}/external`);
      if (!response.ok) {
        throw new Error('Live traffic data could not be loaded. Try again shortly.');
      }
      const data = await response.json();
      if (!Array.isArray(data)) {
        throw new Error('The live traffic service returned an unexpected response.');
      }
      setTrafficData(data);
      setLastUpdated(new Date());
    } catch (loadError) {
      setError(loadError.message || 'Live traffic data could not be loaded.');
    } finally {
      setLoading(false);
    }
  }

  useEffect(() => {
    const timer = window.setTimeout(loadTrafficData, 0);
    const refreshTimer = window.setInterval(loadTrafficData, 60_000);
    return () => {
      window.clearTimeout(timer);
      window.clearInterval(refreshTimer);
    };
  }, []);

  const filteredTrafficData = trafficData.filter((station) => {
    const query = searchTerm.trim().toLocaleLowerCase();
    const searchValue = `${station.stationName ?? ''} ${station.stationId}`.toLocaleLowerCase();
    return searchValue.includes(query);
  });

  const availableSpeeds = trafficData
    .map((station) => station.averageSpeedKmh)
    .filter(Number.isFinite);
  const averageSpeed = availableSpeeds.length > 0
    ? Math.round(availableSpeeds.reduce((total, speed) => total + speed, 0) / availableSpeeds.length)
    : null;
  const stationsWithSpeed = availableSpeeds.length;

  return (
    <div className="dashboard">
      <Header />
      <main>
        <div className="dashboard-intro">
          <div>
            <span className="eyebrow">FINLAND · LIVE ROAD MONITOR</span>
            <h2>Traffic at a glance</h2>
            <p>Current traffic volume and speed readings from Finnish road stations.</p>
          </div>
          <span className="source-pill"><span aria-hidden="true">●</span> Digitraffic data</span>
        </div>

        <section className="live-traffic-section" aria-labelledby="live-data-heading">
          <div className="section-heading">
            <div>
              <span className="eyebrow">LIVE MEASUREMENTS</span>
              <h2 id="live-data-heading">Road stations</h2>
              <p>
                Station names and measurements are supplied by Digitraffic.
                {lastUpdated && ` Updated ${lastUpdated.toLocaleTimeString()}.`}
              </p>
            </div>
            <button type="button" onClick={loadTrafficData} disabled={loading}>
              {loading ? 'Updating…' : 'Refresh data'}
            </button>
          </div>

          <section className="stats" aria-label="Live traffic summary">
            <StatCard title="Reporting stations" value={trafficData.length} />
            <StatCard title="Stations with speed" value={stationsWithSpeed} />
            <StatCard title="Average reported speed" value={averageSpeed === null ? 'Not available' : `${averageSpeed} km/h`} />
          </section>

          <div className="station-toolbar">
            <label htmlFor="station-search">Find a station</label>
            <input
              id="station-search"
              type="search"
              placeholder="Search by station name or ID"
              value={searchTerm}
              onChange={(event) => setSearchTerm(event.target.value)}
            />
            <span>{filteredTrafficData.length} of {trafficData.length} stations</span>
          </div>

          {loading && trafficData.length === 0 && (
            <p className="empty-state" role="status">Loading current station readings…</p>
          )}
          {error && <p className="error-message" role="alert">{error}</p>}
          {!loading && !error && trafficData.length === 0 && (
            <p className="empty-state">No live station measurements are available right now.</p>
          )}
          {trafficData.length > 0 && filteredTrafficData.length === 0 && (
            <p className="empty-state">No stations match this search.</p>
          )}

          {filteredTrafficData.length > 0 && (
            <div className="table-wrapper">
              <table>
                <thead>
                  <tr>
                    <th>Station</th>
                    <th>Vehicles / hour</th>
                    <th>Average speed</th>
                    <th>Measured time</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredTrafficData.map((station) => (
                    <tr key={station.stationId}>
                      <td>
                        <strong>{station.stationName?.replaceAll('_', ' ') || `Station ${station.stationId}`}</strong>
                        <span className="station-id">Station ID {station.stationId}</span>
                      </td>
                      <td>{Number.isFinite(station.vehiclesPerHour)
                        ? station.vehiclesPerHour.toLocaleString()
                        : 'Not available'}</td>
                      <td>{Number.isFinite(station.averageSpeedKmh)
                        ? `${station.averageSpeedKmh} km/h`
                        : 'Not available'}</td>
                      <td>{station.measuredTime
                        ? new Date(station.measuredTime).toLocaleString()
                        : 'Not available'}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <Suspense fallback={<p className="empty-state">Preparing traffic charts…</p>}>
          <TrafficCharts data={trafficData} />
        </Suspense>

        <p className="attribution">
          Live road traffic data provided by Digitraffic / Fintraffic.
        </p>
      </main>
    </div>
  );
}

export default App;
