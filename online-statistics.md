# Online statistics

`OnlineDescriptiveStatistics` uses three online algorithms:

- **Welford's algorithm** updates mean and the second central moment (`m2`) on each `Update(double value)` call. Variance is `m2 / n` (population variance), and standard deviation is `sqrt(variance)`.
- **Running extrema** track minimum and maximum with `Math.Min` / `Math.Max`.
- **P² (P-square) quantile estimator** tracks the median (`0.5`) and percentiles (`0.05`, `0.95`) using five moving markers per quantile, without storing all observed values.

The implementation rejects non-finite inputs (`NaN`, `+∞`, `-∞`) by throwing `ArgumentOutOfRangeException`.
