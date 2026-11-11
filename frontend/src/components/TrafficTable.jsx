
function TrafficTable({ data }) {
  return (
    <section className="table-section">
      <h2>Traffic Measurements</h2>

      <div className="table-wrapper">
        <table>
          <thead>
            <tr>
              <th>Location</th>
              <th>Vehicles</th>
              <th>Average Speed</th>
              <th>Status</th>
            </tr>
          </thead>

          <tbody>
            {data.map((item) => (
              <tr key={item.id}>
                <td>{item.location}</td>
                <td>{item.vehicles.toLocaleString()}</td>
                <td>{item.speed} km/h</td>
                <td>
                  <span
                    className={
                      item.status === 'Heavy'
                        ? 'status-heavy'
                        : 'status-normal'
                    }
                  >
                    {item.status}
                  </span>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export default TrafficTable;
