
import './App.css';

import Header from './components/Header';
import StatCard from './components/StatCard';
import TrafficTable from './components/TrafficTable';

import trafficData from './data/trafficData';

function App() {
  // Calculate total vehicles
  const totalVehicles = trafficData.reduce(
    (total, item) => total + item.vehicles,
    0
  );

  // Calculate average speed
  const averageSpeed = Math.round(
    trafficData.reduce(
      (total, item) => total + item.speed,
      0
    ) / trafficData.length
  );

  return (
    <div className="dashboard">

      <Header />

      <main>
        <h2>Traffic Overview</h2>

        <p className="subtitle">
          Monitor traffic measurements across Finland.
        </p>

        <section className="stats">

          <StatCard
            title="Total Vehicles"
            value={totalVehicles.toLocaleString()}
          />

          <StatCard
            title="Measurement Points"
            value={trafficData.length}
          />

          <StatCard
            title="Average Speed"
            value={`${averageSpeed} km/h`}
          />

        </section>

        <TrafficTable data={trafficData} />

      </main>
    </div>
  );
}

export default App;
