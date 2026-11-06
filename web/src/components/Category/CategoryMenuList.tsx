import { Children, forwardRef, type ReactNode } from "react";
import { Box, Divider, MenuList, type MenuListProps } from "@mui/material";

/**
 * @brief Scrollable category options list without the management footer.
 */
export const CategoryOptionsList = forwardRef<HTMLUListElement, MenuListProps>(
  function CategoryOptionsList({ children, ...props }, ref) {
    return (
      <MenuList ref={ref} {...props}>
        <Box component="div" className="ledger-category-options-scroll">
          {children}
        </Box>
      </MenuList>
    );
  },
);

/**
 * @brief Scrollable category options list with the management footer.
 */
export const CategoryMenuList = forwardRef<HTMLUListElement, MenuListProps>(
  function CategoryMenuList({ children, ...props }, ref) {
    const menuItems = Children.toArray(children);
    const manageItem = menuItems.pop() as ReactNode;

    return (
      <MenuList ref={ref} {...props}>
        <Box component="div" className="ledger-category-options-scroll">
          {menuItems}
        </Box>
        <Divider />
        {manageItem}
      </MenuList>
    );
  },
);
