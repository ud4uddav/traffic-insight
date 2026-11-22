import { useState } from "react";

function AddTrafficForm({ onAdd }) {
  const [location, setLocation] = useState("");
  const [vehicles, setVehicles] = useState("");
  const [speed, setSpeed] = useState("");
  const [status, setStatus] = useState("Normal");

  const [error, setError] = useState("");

  async function handleSubmit(event) {
    event.preventDefault();

    setError("");

    const newMeasurement = {
      location,
      vehicles: Number(vehicles),
      speed: Number(speed),
      status,
    };

    try {
      await onAdd(newMeasurement);

      setLocation("");
      setVehicles("");
      setSpeed("");
      setStatus("Normal");
    } catch (error) {
      setError(error.message);
    }
  }

  return (
    <section className="form-section">
      <h2>Add Traffic Measurement</h2>

      <form onSubmit={handleSubmit}>
        <input
          type="text"
          placeholder="Location"
          value={location}
          onChange={(event) => setLocation(event.target.value)}
          required
        />

        <input
          type="number"
          placeholder="Number of vehicles"
          value={vehicles}
          onChange={(event) => setVehicles(event.target.value)}
          min="0"
          max="100000"
          required
        />

        <input
          type="number"
          placeholder="Average speed (km/h)"
          value={speed}
          onChange={(event) => setSpeed(event.target.value)}
          min="0"
          max="300"
          required
        />

        <select
          value={status}
          onChange={(event) => setStatus(event.target.value)}
        >
          <option value="Normal">Normal</option>
          <option value="Heavy">Heavy</option>
        </select>

        <button type="submit">Add Measurement</button>

        {error && <p className="error-message">{error}</p>}
      </form>
    </section>
  );
}

export default AddTrafficForm;
