"use client";

import ReactEcharts from "echarts-for-react";
import type { EChartsOption } from "echarts";
import type { CSSProperties } from "react";

type Props = {
  option: EChartsOption;
  className?: string;
  style?: CSSProperties;
  /** Default height matches previous dashboard chart rows (h-48). */
  height?: number;
};

/**
 * Apache ECharts with explicit dimensions. Theme colors must be resolved to real values
 * before passing `option` (see `useChartTheme` + `dashboard-echarts` builders).
 */
export function ThemedECharts({ option, className, style, height = 192 }: Props) {
  return (
    <ReactEcharts
      option={option}
      className={className}
      style={{ width: "100%", height, ...style }}
      opts={{ renderer: "canvas" }}
      notMerge
      lazyUpdate
    />
  );
}
