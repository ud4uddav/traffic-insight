
function Header() {
  return (
    <header className="header">
      <div>
        <h1>Traffic Insight</h1>
        <p>Finnish road traffic, measured live</p>
      </div>

      <span className="live-status">
        <span aria-hidden="true">●</span> Digitraffic live
      </span>
    </header>
  );
}

export default Header;
