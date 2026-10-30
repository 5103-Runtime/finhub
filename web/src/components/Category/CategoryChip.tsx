import { type CSSProperties } from "react";
import { Box } from "@mui/material";
import { getCategoryColor } from "@app/utils/categories";
import "./CategoryChip.css";

type CategoryChipProps = {
  name: string;
  className?: string;
};

export function CategoryChip({ name, className }: CategoryChipProps) {
  return (
    <Box
      component="span"
      className={["category-chip", className].filter(Boolean).join(" ")}
      title={name}
      style={
        {
          "--category-color": getCategoryColor(name),
        } as CSSProperties
      }
    >
      {name}
    </Box>
  );
}
