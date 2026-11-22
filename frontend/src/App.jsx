
import { useEffect, useState } from 'react';

import './App.css';

import Header from './components/Header';
import StatCard from './components/StatCard';
import TrafficTable from './components/TrafficTable';

const API_URL = import.meta.env.VITE_API_URL;

function App() {
  // Traffic data and application state
  const [trafficData, setTrafficData] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [successMessage, setSuccessMessage] = useState('');

  // Search and filter state
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState('All');

  // Form state
  const [location, setLocation] = useState('');
  const [vehicles, setVehicles] = useState('');
  const [speed, setSpeed] = useState('');
  const [status, setStatus] = useState('Normal');

  // Editing state
  const [editingId, setEditingId] = useState(null);

  // Fetch traffic measurements from backend
  async function loadTrafficData() {
    setLoading(true);
    setError('');

    try {
      const response = await fetch(API_URL);

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

  // Load data when application starts
  useEffect(() => {
    loadTrafficData();
  }, []);

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

  // Reset form fields
  function resetForm() {
    setEditingId(null);
    setLocation('');
    setVehicles('');
    setSpeed('');
    setStatus('Normal');
  }

  // Validate traffic measurement form
  function validateMeasurement() {
    if (!location.trim()) {
      return 'Location is required';
    }

    if (
      vehicles === '' ||
      !Number.isInteger(Number(vehicles)) ||
      Number(vehicles) < 0
    ) {
      return 'Vehicles must be a non-negative whole number';
    }

    if (
      speed === '' ||
      !Number.isFinite(Number(speed)) ||
      Number(speed) < 0 ||
      Number(speed) > 300
    ) {
      return 'Speed must be between 0 and 300 km/h';
    }

    if (!['Normal', 'Heavy'].includes(status)) {
      return 'Invalid traffic status';
    }

    return null;
  }

  // Load an existing measurement into the form
  function handleEditMeasurement(measurement) {
    setEditingId(measurement.id);
    setLocation(measurement.location);
    setVehicles(String(measurement.vehicles));
    setSpeed(String(measurement.speed));
    setStatus(measurement.status);

    window.scrollTo({
      top: 0,
      behavior: 'smooth',
    });
  }

  // Add a new traffic measurement
  async function handleAddMeasurement(event) {
    event.preventDefault();

    const validationError = validateMeasurement();

    if (validationError) {
      setError(validationError);
      setSuccessMessage('');
      return;
    }

    setError('');
    setSuccessMessage('');

    try {
      const response = await fetch(API_URL, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          location: location.trim(),
          vehicles: Number(vehicles),
          speed: Number(speed),
          status,
        }),
      });

      if (!response.ok) {
        const errorData = await response.json();

        const message = Array.isArray(errorData.errors)
          ? errorData.errors.join(' ')
          : 'Failed to add measurement';

        throw new Error(message);
      }

      const createdMeasurement = await response.json();

      setTrafficData((currentData) => [
        ...currentData,
        createdMeasurement,
      ]);

      resetForm();
      setSuccessMessage('Traffic measurement added successfully!');
    } catch (error) {
      setError(error.message);
    }
  }

  // Update an existing traffic measurement
  async function handleUpdateMeasurement(event) {
    event.preventDefault();

    const validationError = validateMeasurement();

    if (validationError) {
      setError(validationError);
      setSuccessMessage('');
      return;
    }

    setError('');
    setSuccessMessage('');

    try {
      const response = await fetch(`${API_URL}/${editingId}`, {
        method: 'PUT',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify({
          location: location.trim(),
          vehicles: Number(vehicles),
          speed: Number(speed),
          status,
        }),
      });

      if (!response.ok) {
        const errorMessage = await response.text();
        throw new Error(
          errorMessage || 'Failed to update measurement'
        );
      }

      const updatedMeasurement = await response.json();

      setTrafficData((currentData) =>
        currentData.map((item) =>
          item.id === editingId ? updatedMeasurement : item
        )
      );

      resetForm();
      setSuccessMessage('Traffic measurement updated successfully!');
    } catch (error) {
      setError(error.message);
    }
  }

  // Delete a traffic measurement
  async function handleDeleteMeasurement(id) {
    const confirmed = window.confirm(
      'Are you sure you want to delete this measurement?'
    );

    if (!confirmed) {
      return;
    }

    setError('');
    setSuccessMessage('');

    try {
      const response = await fetch(`${API_URL}/${id}`, {
        method: 'DELETE',
      });

      if (!response.ok) {
        const errorMessage = await response.text();
        throw new Error(
          errorMessage || 'Failed to delete measurement'
        );
      }

      setTrafficData((currentData) =>
        currentData.filter((item) => item.id !== id)
      );

      if (editingId === id) {
        resetForm();
      }

      setSuccessMessage('Traffic measurement deleted successfully!');
    } catch (error) {
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

        {/* Refresh traffic data */}
        <button
          type="button"
          onClick={loadTrafficData}
          disabled={loading}
        >
          {loading ? 'Refreshing...' : 'Refresh Data'}
        </button>

        {/* Loading, error and success messages */}
        {loading && (
          <p>Loading traffic measurements...</p>
        )}

        {error && (
          <p className="error-message">
            Error: {error}
          </p>
        )}

        {successMessage && (
          <p className="success-message">
            {successMessage}
          </p>
        )}

        {/* Dashboard statistics */}
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

        {/* Search and filter */}
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

        {/* Add or edit measurement form */}
        <section className="form-section">
          <h2>
            {editingId !== null
              ? 'Edit Traffic Measurement'
              : 'Add Traffic Measurement'}
          </h2>

          <form
            onSubmit={
              editingId !== null
                ? handleUpdateMeasurement
                : handleAddMeasurement
            }
          >
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
              max="100000"
              value={vehicles}
              onChange={(event) => setVehicles(event.target.value)}
              required
            />

            <input
              type="number"
              placeholder="Average speed (km/h)"
              min="0"
              max="300"
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
              {editingId !== null
                ? 'Update Measurement'
                : 'Add Measurement'}
            </button>

            {editingId !== null && (
              <button
                type="button"
                onClick={resetForm}
              >
                Cancel Editing
              </button>
            )}
          </form>
        </section>

        {/* Traffic measurements table */}
        <TrafficTable
          data={filteredTrafficData}
          onEdit={handleEditMeasurement}
          onDelete={handleDeleteMeasurement}
        />

        <p className="result-count">
          Showing {filteredTrafficData.length} of {trafficData.length} locations
        </p>
      </main>
    </div>
  );
}

export default App;
