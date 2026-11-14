
import { useEffect, useState } from 'react';

import './App.css';

import Header from './components/Header';
import StatCard from './components/StatCard';
import TrafficTable from './components/TrafficTable';



function App() {
  // Store traffic measurements in React state
const [trafficData, setTrafficData] = useState([]);
const [loading, setLoading] = useState(true);
const [error, setError] = useState('');

useEffect(() => {
  async function loadTrafficData() {
    try {
      const response = await fetch(
        'http://localhost:5278/api/traffic'
      );

      if (!response.ok) {
        throw new Error('Failed to fetch traffic data');
      }

      const data = await response.json();

      setTrafficData(data);
    } catch (error) {
      setError(error.message);
    } finally {
      setLoading(false);
    }
  }

  loadTrafficData();
}, []);


  // Store search input
  const [searchTerm, setSearchTerm] = useState('');

  // Store selected traffic status
  const [statusFilter, setStatusFilter] = useState('All');

  // Store new measurement form values
  const [location, setLocation] = useState('');
  const [vehicles, setVehicles] = useState('');
  const [speed, setSpeed] = useState('');
  const [status, setStatus] = useState('Normal');

  // Calculate total vehicles
  const totalVehicles = trafficData.reduce(
    (total, item) => total + item.vehicles,
    0
  );

  // Calculate average speed
  const averageSpeed =
    trafficData.length > 0
      ? Math.round(
          trafficData.reduce(
            (total, item) => total + item.speed,
            0
          ) / trafficData.length
        )
      : 0;

  // Search and filter traffic measurements
  const filteredTrafficData = trafficData.filter((item) => {
    const matchesSearch = item.location
      .toLowerCase()
      .includes(searchTerm.toLowerCase());

    const matchesStatus =
      statusFilter === 'All' || item.status === statusFilter;

    return matchesSearch && matchesStatus;
  });

  // Add a new traffic measurement
  
async function handleAddMeasurement(event) {
  // Stop the browser from refreshing the page
  event.preventDefault();

  // Clear any previous error
  setError('');

  try {
    // Send the new measurement to the C# backend
    const response = await fetch(
      'http://localhost:5278/api/traffic',
      {
        method: 'POST',

        headers: {
          'Content-Type': 'application/json',
        },

        body: JSON.stringify({
          location: location.trim(),
          vehicles: Number(vehicles),
          speed: Number(speed),
          status: status,
        }),
      }
    );

    // Check whether the backend accepted the request
    if (!response.ok) {
      const errorMessage = await response.text();
      throw new Error(errorMessage || 'Failed to add measurement');
    }

    // Read the newly created measurement from the backend
    const createdMeasurement = await response.json();

    // Update the React dashboard with the backend response
    setTrafficData((currentData) => [
      ...currentData,
      createdMeasurement,
    ]);

    // Clear the form
    setLocation('');
    setVehicles('');
    setSpeed('');
    setStatus('Normal');

  } catch (error) {
    // Display an error if the request fails
    setError(error.message);
  }
}


  return (
    <div className="dashboard">

      <Header />

      <main>
        <h2>Traffic Overview</h2>

        <p className="subtitle">
          Monitor traffic measurements across Finland.
        </p>
        {loading && <p>Loading traffic measurements...</p>}

        {error && <p className="error-message">Error: {error}</p>}

        {/* Statistics */}
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

        {/* Search and Filter */}
        <section className="controls">

          <input
            type="text"
            placeholder="Search traffic location..."
            value={searchTerm}
            onChange={(event) => setSearchTerm(event.target.value)}
          />

          <select
            value={statusFilter}
            onChange={(event) => setStatusFilter(event.target.value)}
          >
            <option value="All">All Traffic</option>
            <option value="Normal">Normal</option>
            <option value="Heavy">Heavy</option>
          </select>

        </section>

        {/* Add Measurement Form */}
        <section className="form-section">

          <h2>Add Traffic Measurement</h2>

          <form onSubmit={handleAddMeasurement}>

            <input
              type="text"
              placeholder="Location name"
              value={location}
              onChange={(event) => setLocation(event.target.value)}
              required
            />

            <input
              type="number"
              placeholder="Number of vehicles"
              min="0"
              value={vehicles}
              onChange={(event) => setVehicles(event.target.value)}
              required
            />

            <input
              type="number"
              placeholder="Average speed (km/h)"
              min="0"
              value={speed}
              onChange={(event) => setSpeed(event.target.value)}
              required
            />

            <select
              value={status}
              onChange={(event) => setStatus(event.target.value)}
            >
              <option value="Normal">Normal</option>
              <option value="Heavy">Heavy</option>
            </select>

            <button type="submit">
              Add Measurement
            </button>

          </form>

        </section>

        {/* Traffic Table */}
        <TrafficTable data={filteredTrafficData} />

        <p className="result-count">
          Showing {filteredTrafficData.length} of {trafficData.length} locations
        </p>

      </main>
    </div>
  );
}

export default App;
