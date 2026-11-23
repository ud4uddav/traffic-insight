import assert from 'node:assert/strict';
import test from 'node:test';
import { averageReportedSpeed } from '../src/utils/trafficMetrics.js';

test('averages finite speed readings and ignores missing readings', () => {
  const stations = [
    { averageSpeedKmh: 80 },
    { averageSpeedKmh: null },
    { averageSpeedKmh: 91 },
    { averageSpeedKmh: undefined },
  ];

  assert.equal(averageReportedSpeed(stations), 86);
});

test('returns null when no station reports speed', () => {
  assert.equal(averageReportedSpeed([{ averageSpeedKmh: null }]), null);
  assert.equal(averageReportedSpeed([]), null);
});

test('keeps a reported speed of zero as a real measurement', () => {
  assert.equal(averageReportedSpeed([{ averageSpeedKmh: 0 }, { averageSpeedKmh: null }]), 0);
});
