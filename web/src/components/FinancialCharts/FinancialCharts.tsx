import { Box, Card, CardContent, Typography } from "@mui/material";
import { useState, type ReactNode } from "react";
import type {
  OverviewCategorySpending,
  OverviewMonthlyPoint,
} from "@app/services/overviewApi";
import "./FinancialCharts.css";

/**
 * Provides the currency and formatting behavior shared by dashboard charts.
 */
type ChartProps = {
  currency: string;
  formatValue: (value: number) => string;
};

/**
 * Provides monthly points to the income and spending line chart.
 */
type CashFlowChartProps = ChartProps & {
  points: OverviewMonthlyPoint[];
};

/**
 * Provides category totals to the spending category chart.
 */
type CategoryChartProps = ChartProps & {
  categories: OverviewCategorySpending[];
};

/**
 * Provides monthly points to the net cash-flow bar chart.
 */
type NetCashFlowChartProps = ChartProps & {
  points: OverviewMonthlyPoint[];
};

/**
 * The width of the shared SVG chart viewport.
 */
const CHART_WIDTH = 720;
/**
 * The height of the income and spending chart viewport.
 */
const CHART_HEIGHT = 250;
/**
 * Defines geometry and interaction dimensions for the income and spending chart.
 */
const LINE_CHART = {
  plotLeft: 30,
  plotRight: 36,
  plotTop: 22,
  plotBottom: 38,
  axisValueLabelY: 14,
  monthLabelOffset: 25,
  pointHitRadius: 12,
  pointRadius: 4,
} as const;
/**
 * Defines geometry, label spacing, and bar dimensions for the net cash-flow chart.
 */
const NET_CHART = {
  plotLeft: 58,
  plotRight: 58,
  plotTop: 28,
  valueLabelBand: 18,
  barPlotHalfHeight: 70,
  positiveLabelGap: 10,
  negativeLabelGap: 16,
  zeroLabelOffset: 6,
  dateLabelGap: 24,
  bottomInset: 18,
  barMaxWidth: 54,
  barGap: 12,
  barMinHeight: 1,
  barRadius: 3,
} as const;
/**
 * Defines sizing and spacing used by line-chart hover tooltips.
 */
const TOOLTIP = {
  edgeMargin: 4,
  characterWidth: 6,
  horizontalPadding: 8,
  height: 38,
  verticalGap: 8,
  lineHeight: 15,
} as const;
const CATEGORY_DISPLAY_LIMIT = 6;

/**
 * Rounds chart values up to a stable, readable axis maximum.
 *
 * @param values The values represented by the chart.
 * @returns A rounded positive maximum suitable for chart scaling.
 */
function getNiceMaximum(values: number[]) {
  const maximum = Math.max(...values, 1);
  return Math.ceil(maximum / 100) * 100 || 100;
}

/**
 * Calculates an x-coordinate for a chart point within a horizontal plot.
 *
 * @param index The zero-based point index.
 * @param count The total number of points.
 * @param left The plot's left inset.
 * @param right The plot's right inset.
 * @returns The point's x-coordinate in the SVG viewport.
 */
function getPlotX(index: number, count: number, left: number, right: number) {
  const plotWidth = CHART_WIDTH - left - right;
  return count <= 1
    ? left + plotWidth / 2
    : left + (index / (count - 1)) * plotWidth;
}

/**
 * Calculates a y-coordinate for a line-chart value.
 *
 * @param value The value to position.
 * @param maximum The chart's scale maximum.
 * @returns The value's y-coordinate in the SVG viewport.
 */
function getY(value: number, maximum: number) {
  const plotHeight = CHART_HEIGHT - LINE_CHART.plotTop - LINE_CHART.plotBottom;
  return LINE_CHART.plotTop + plotHeight - (value / maximum) * plotHeight;
}

/**
 * Renders the shared card shell used by dashboard visualizations.
 *
 * @param props The chart title, description, and visualization content.
 * @returns A dashboard chart card.
 */
function ChartCard({
  title,
  description,
  children,
}: {
  title: string;
  description: string;
  children: ReactNode;
}) {
  return (
    <Card className="dashboard-chart-card">
      <CardContent>
        <Typography className="dashboard-chart-title">{title}</Typography>
        <Typography className="dashboard-chart-description">
          {description}
        </Typography>
        {children}
      </CardContent>
    </Card>
  );
}

/**
 * Renders monthly income and spending as two trend lines.
 * @param props The monthly data and currency formatting behavior.
 * @returns The income and spending line-chart card.
 */
export function CashFlowChart({
  points,
  currency,
  formatValue,
}: CashFlowChartProps) {
  const [hoveredPoint, setHoveredPoint] = useState<{
    index: number;
    series: "income" | "spending";
  } | null>(null);
  const [hoveredSeries, setHoveredSeries] = useState<
    "income" | "spending" | null
  >(null);
  const values = points.flatMap((point) => [point.income, point.spending]);
  const maximum = getNiceMaximum(values);
  const incomePath = points
    .map(
      (point, index) =>
        `${getPlotX(index, points.length, LINE_CHART.plotLeft, LINE_CHART.plotRight)},${getY(point.income, maximum)}`,
    )
    .join(" ");
  const spendingPath = points
    .map(
      (point, index) =>
        `${getPlotX(index, points.length, LINE_CHART.plotLeft, LINE_CHART.plotRight)},${getY(point.spending, maximum)}`,
    )
    .join(" ");

  return (
    <ChartCard
      title="Income and spending"
      description={`Monthly cash flow for ${currency}.`}
    >
      {points.length === 0 ? (
        <Typography className="dashboard-chart-empty">
          No transactions in this period.
        </Typography>
      ) : (
        <Box className="dashboard-chart-scroll">
          <svg
            className="dashboard-chart"
            viewBox={`0 0 ${CHART_WIDTH} ${CHART_HEIGHT}`}
            role="img"
            aria-label={`Line chart comparing monthly income and spending in ${currency}`}
          >
            <line
              x1={LINE_CHART.plotLeft}
              x2={CHART_WIDTH - LINE_CHART.plotRight}
              y1={CHART_HEIGHT - LINE_CHART.plotBottom}
              y2={CHART_HEIGHT - LINE_CHART.plotBottom}
              className="chart-axis"
            />
            <line
              x1={LINE_CHART.plotLeft}
              x2={LINE_CHART.plotLeft}
              y1={LINE_CHART.plotTop}
              y2={CHART_HEIGHT - LINE_CHART.plotBottom}
              className="chart-axis"
            />
            <text
              x={LINE_CHART.plotLeft}
              y={LINE_CHART.axisValueLabelY}
              className="chart-axis-label"
            >
              {formatValue(maximum)}
            </text>
            {points.map((point, index) => (
              <text
                key={`${point.month}-label`}
                x={getPlotX(
                  index,
                  points.length,
                  LINE_CHART.plotLeft,
                  LINE_CHART.plotRight,
                )}
                y={
                  CHART_HEIGHT -
                  LINE_CHART.plotBottom +
                  LINE_CHART.monthLabelOffset
                }
                textAnchor="middle"
                className="chart-axis-label"
              >
                {point.label}
              </text>
            ))}
            <polyline
              points={incomePath}
              className="chart-line-hit-area"
              onMouseEnter={() => setHoveredSeries("income")}
              onMouseLeave={() => setHoveredSeries(null)}
            />
            <polyline
              points={spendingPath}
              className="chart-line-hit-area"
              onMouseEnter={() => setHoveredSeries("spending")}
              onMouseLeave={() => setHoveredSeries(null)}
            />
            <polyline
              points={incomePath}
              className={`chart-line chart-line-income ${
                hoveredSeries === "income" ? "hovered" : ""
              }`}
            />
            <polyline
              points={spendingPath}
              className={`chart-line chart-line-spending ${
                hoveredSeries === "spending" ? "hovered" : ""
              }`}
            />
            {points.map((point, index) => (
              <g key={point.month}>
                <circle
                  cx={getPlotX(
                    index,
                    points.length,
                    LINE_CHART.plotLeft,
                    LINE_CHART.plotRight,
                  )}
                  cy={getY(point.income, maximum)}
                  r={LINE_CHART.pointHitRadius}
                  className="chart-point-hit-area"
                  onMouseEnter={() =>
                    (() => {
                      setHoveredSeries("income");
                      setHoveredPoint({ index, series: "income" });
                    })()
                  }
                  onMouseLeave={() => {
                    setHoveredSeries(null);
                    setHoveredPoint(null);
                  }}
                />
                <circle
                  cx={getPlotX(
                    index,
                    points.length,
                    LINE_CHART.plotLeft,
                    LINE_CHART.plotRight,
                  )}
                  cy={getY(point.income, maximum)}
                  r={LINE_CHART.pointRadius}
                  className={`chart-point chart-point-income ${
                    hoveredPoint?.index === index &&
                    hoveredPoint.series === "income"
                      ? "hovered"
                      : ""
                  }`}
                />
                <circle
                  cx={getPlotX(
                    index,
                    points.length,
                    LINE_CHART.plotLeft,
                    LINE_CHART.plotRight,
                  )}
                  cy={getY(point.spending, maximum)}
                  r={LINE_CHART.pointHitRadius}
                  className="chart-point-hit-area"
                  onMouseEnter={() =>
                    (() => {
                      setHoveredSeries("spending");
                      setHoveredPoint({ index, series: "spending" });
                    })()
                  }
                  onMouseLeave={() => {
                    setHoveredSeries(null);
                    setHoveredPoint(null);
                  }}
                />
                <circle
                  cx={getPlotX(
                    index,
                    points.length,
                    LINE_CHART.plotLeft,
                    LINE_CHART.plotRight,
                  )}
                  cy={getY(point.spending, maximum)}
                  r={LINE_CHART.pointRadius}
                  className={`chart-point chart-point-spending ${
                    hoveredPoint?.index === index &&
                    hoveredPoint.series === "spending"
                      ? "hovered"
                      : ""
                  }`}
                />
              </g>
            ))}
            {hoveredPoint
              ? (() => {
                  const point = points[hoveredPoint.index];
                  const value =
                    hoveredPoint.series === "income"
                      ? point.income
                      : point.spending;
                  const x = getPlotX(
                    hoveredPoint.index,
                    points.length,
                    LINE_CHART.plotLeft,
                    LINE_CHART.plotRight,
                  );
                  const y = getY(value, maximum);
                  const tooltipLabel = `${point.label} ${hoveredPoint.series}`;
                  const tooltipValue = formatValue(value);
                  const tooltipWidth =
                    Math.max(tooltipLabel.length, tooltipValue.length) *
                      TOOLTIP.characterWidth +
                    TOOLTIP.horizontalPadding;
                  const tooltipX = Math.max(
                    TOOLTIP.edgeMargin,
                    Math.min(
                      x - tooltipWidth / 2,
                      CHART_WIDTH - tooltipWidth - TOOLTIP.edgeMargin,
                    ),
                  );
                  const tooltipY = Math.max(
                    TOOLTIP.edgeMargin,
                    y - TOOLTIP.height - TOOLTIP.verticalGap,
                  );
                  const tooltipCenter = tooltipX + tooltipWidth / 2;

                  return (
                    <g className="chart-tooltip" pointerEvents="none">
                      <rect
                        x={tooltipX}
                        y={tooltipY}
                        width={tooltipWidth}
                        height={TOOLTIP.height}
                        rx="5"
                      />
                      <text
                        x={tooltipCenter}
                        y={tooltipY + TOOLTIP.lineHeight}
                        textAnchor="middle"
                      >
                        <tspan x={tooltipCenter}>{tooltipLabel}</tspan>
                        <tspan x={tooltipCenter} dy={TOOLTIP.lineHeight}>
                          {tooltipValue}
                        </tspan>
                      </text>
                    </g>
                  );
                })()
              : null}
          </svg>
        </Box>
      )}
      <Box className="dashboard-chart-legend">
        <span className="chart-legend-income">Income</span>
        <span className="chart-legend-spending">Spending</span>
      </Box>
    </ChartCard>
  );
}

/**
 * Renders the largest spending categories as proportional horizontal bars.
 *
 * @param props The category totals and currency formatting behavior.
 * @returns The category spending chart card.
 */
export function CategorySpendingChart({
  categories,
  currency,
  formatValue,
}: CategoryChartProps) {
  const visibleCategories = categories.slice(0, CATEGORY_DISPLAY_LIMIT);
  const maximum = getNiceMaximum(
    visibleCategories.map((category) => category.amount),
  );

  return (
    <ChartCard
      title="Spending by category"
      description={`Largest spending categories for ${currency}.`}
    >
      {visibleCategories.length === 0 ? (
        <Typography className="dashboard-chart-empty">
          No spending has been recorded in this period.
        </Typography>
      ) : (
        <Box className="category-bars">
          {visibleCategories.map((category) => (
            <Box
              className="category-bar-row"
              key={`${category.currency}-${category.categoryId}`}
            >
              <Typography className="category-bar-label">
                {category.categoryName}
              </Typography>
              <Box className="category-bar-track">
                <Box
                  className="category-bar-fill"
                  sx={{ width: `${(category.amount / maximum) * 100}%` }}
                />
              </Box>
              <Typography className="category-bar-value">
                {formatValue(category.amount)}
              </Typography>
            </Box>
          ))}
        </Box>
      )}
    </ChartCard>
  );
}

/**
 * Renders monthly net income as positive or negative bars.
 *
 * @param props The monthly data and currency formatting behavior.
 * @returns The net cash-flow chart card.
 */
export function NetCashFlowChart({
  points,
  currency,
  formatValue,
}: NetCashFlowChartProps) {
  const [hoveredIndex, setHoveredIndex] = useState<number | null>(null);
  const maximum = getNiceMaximum(points.map((point) => Math.abs(point.net)));
  const baseline =
    NET_CHART.plotTop + NET_CHART.valueLabelBand + NET_CHART.barPlotHalfHeight;
  const getNetY = (value: number) =>
    baseline - (value / maximum) * NET_CHART.barPlotHalfHeight;
  const lowestNegativeLabelY = Math.max(
    ...points
      .filter((point) => point.net < 0)
      .map((point) => getNetY(point.net) + NET_CHART.negativeLabelGap),
    baseline,
  );
  const dateY = lowestNegativeLabelY + NET_CHART.dateLabelGap;
  const chartHeight = dateY + NET_CHART.bottomInset;
  const barWidth = points.length
    ? Math.min(
        NET_CHART.barMaxWidth,
        (CHART_WIDTH - NET_CHART.plotLeft - NET_CHART.plotRight) /
          points.length -
          NET_CHART.barGap,
      )
    : 0;

  return (
    <ChartCard
      title="Net cash flow"
      description={`Monthly income minus spending for ${currency}.`}
    >
      {points.length === 0 ? (
        <Typography className="dashboard-chart-empty">
          No monthly cash flow has been recorded in this period.
        </Typography>
      ) : (
        <Box className="dashboard-chart-scroll">
          <svg
            className="dashboard-chart net-cash-flow-chart"
            viewBox={`0 0 ${CHART_WIDTH} ${chartHeight}`}
            role="img"
            aria-label={`Bar chart showing monthly net cash flow in ${currency}`}
          >
            <line
              x1={NET_CHART.plotLeft}
              x2={CHART_WIDTH - NET_CHART.plotRight}
              y1={baseline}
              y2={baseline}
              className="chart-axis"
            />
            {points.map((point, index) => {
              const center = getPlotX(
                index,
                points.length,
                NET_CHART.plotLeft,
                NET_CHART.plotRight,
              );
              const y = getNetY(point.net);
              const height = Math.abs(y - baseline);
              const isNegative = point.net < 0;
              const isZero = point.net === 0;

              return (
                <g key={point.month}>
                  <rect
                    x={center - barWidth / 2}
                    y={Math.min(y, baseline)}
                    width={barWidth}
                    height={Math.max(height, NET_CHART.barMinHeight)}
                    rx={NET_CHART.barRadius}
                    className={`net-cash-flow-bar ${
                      isNegative ? "negative" : ""
                    } ${hoveredIndex === index ? "hovered" : ""}`}
                    onMouseEnter={() => setHoveredIndex(index)}
                    onMouseLeave={() => setHoveredIndex(null)}
                  />
                  <text
                    x={center}
                    y={
                      isZero
                        ? baseline - NET_CHART.zeroLabelOffset
                        : isNegative
                          ? y + NET_CHART.negativeLabelGap
                          : y - NET_CHART.positiveLabelGap
                    }
                    textAnchor="middle"
                    className={`net-cash-flow-value ${isNegative ? "negative" : ""}`}
                  >
                    {formatValue(point.net)}
                  </text>
                  <text
                    x={center}
                    y={dateY}
                    textAnchor="middle"
                    className="chart-axis-label"
                  >
                    {point.label}
                  </text>
                </g>
              );
            })}
          </svg>
        </Box>
      )}
      <Box className="dashboard-chart-legend">
        <span className="chart-legend-net-positive">Positive net</span>
        <span className="chart-legend-net-negative">Negative net</span>
      </Box>
    </ChartCard>
  );
}
