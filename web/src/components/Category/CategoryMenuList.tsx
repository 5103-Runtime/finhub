import { Children, forwardRef, type ReactNode } from "react";
import { Box, MenuList, type MenuListProps } from "@mui/material";

export const CategoryMenuList = forwardRef<HTMLUListElement, MenuListProps>(
  function CategoryMenuList({ children, ...props }, ref) {
    const menuItems = Children.toArray(children);
    const manageItem = menuItems.pop() as ReactNode;
    const divider = menuItems.pop() as ReactNode;

    return (
      <MenuList ref={ref} {...props}>
        <Box component="div" className="ledger-category-options-scroll">
          {menuItems}
        </Box>
        {divider}
        {manageItem}
      </MenuList>
    );
  },
);
