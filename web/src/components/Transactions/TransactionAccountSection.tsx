import {
  Box,
  ButtonBase,
  Collapse,
  IconButton,
  TableSortLabel,
  Typography,
} from "@mui/material";
import CloseRounded from "@mui/icons-material/CloseRounded";
import ExpandMoreRounded from "@mui/icons-material/ExpandMoreRounded";
import type { MouseEvent } from "react";
import type { AccountResponse } from "@app/services/accountsApi";
import type { TransactionResponse } from "@app/services/transactionApi";
import type { TransactionEditFormData } from "@app/validators/transactionSchemas";
import { accountTypeIcons } from "@app/utils/accounts";
import { TransactionEditRow, TransactionRow } from "./TransactionRow";
import type { CategoryOption } from "./categoryTypes";

type SortConfig = {
  key: SortKey;
  ascending: boolean;
} | null;
type SortKey = "transactionDate" | "description" | "amount";

type TransactionAccountSectionProps = {
  account: AccountResponse;
  transactions: TransactionResponse[];
  isCollapsed: boolean;
  sortConfig: SortConfig;
  categoryOptions: CategoryOption[];
  editingId: string | null;
  deletingId: string | null;
  successTransactionId: string | null;
  onToggle: (accountId: string) => void;
  onSort: (key: SortKey, event: MouseEvent<HTMLElement>) => void;
  onClearSort: () => void;
  onCategoryChange: (
    transaction: TransactionResponse,
    categoryId: string,
  ) => Promise<void>;
  onCancelEdit: () => void;
  onSaveEdit: (
    id: string,
    data: TransactionEditFormData,
    confirmPotentialDuplicate: boolean,
  ) => Promise<void>;
  onDelete: (transaction: TransactionResponse) => void;
  onEdit: (transaction: TransactionResponse) => void;
};

export function TransactionAccountSection({
  account,
  transactions,
  isCollapsed,
  sortConfig,
  categoryOptions,
  editingId,
  deletingId,
  successTransactionId,
  onToggle,
  onSort,
  onClearSort,
  onCategoryChange,
  onCancelEdit,
  onSaveEdit,
  onDelete,
  onEdit,
}: TransactionAccountSectionProps) {
  const Icon = accountTypeIcons[account.accountType];
  const accountId = `ledger-account-${account.id}`;
  const panelId = `${accountId}-panel`;

  return (
    <Box id={accountId} className="ledger-account">
      <ButtonBase
        className="ledger-account-header"
        onClick={() => onToggle(account.id)}
        aria-expanded={!isCollapsed}
        aria-controls={panelId}
      >
        <Box className={`account-management-icon ${account.accountType}`}>
          <Icon />
        </Box>
        <Box className="ledger-account-info">
          <Typography className="ledger-account-name">
            {account.name}
          </Typography>
          <Typography className="ledger-account-count">
            {transactions.length}{" "}
            {transactions.length === 1 ? "transaction" : "transactions"}
          </Typography>
        </Box>
        <ExpandMoreRounded
          className={`ledger-chevron${isCollapsed ? " collapsed" : ""}`}
        />
      </ButtonBase>
      <Collapse in={!isCollapsed} id={panelId}>
        {transactions.length === 0 ? (
          <Typography className="ledger-account-empty">
            No transactions in this account yet.
          </Typography>
        ) : (
          <Box className="ledger-list">
            <Box className="ledger-head">
              <span>
                <TableSortLabel
                  className="transaction-sort-label"
                  active={sortConfig?.key === "transactionDate"}
                  direction={
                    sortConfig?.key === "transactionDate" &&
                    sortConfig.ascending
                      ? "asc"
                      : "desc"
                  }
                  onClick={(event) => onSort("transactionDate", event)}
                >
                  Date
                </TableSortLabel>
              </span>
              <span>
                <TableSortLabel
                  className="transaction-sort-label"
                  active={sortConfig?.key === "description"}
                  direction={
                    sortConfig?.key === "description" && sortConfig.ascending
                      ? "asc"
                      : "desc"
                  }
                  onClick={(event) => onSort("description", event)}
                >
                  Description
                </TableSortLabel>
              </span>
              <span>Category</span>
              <span className="ledger-head-amount">
                <TableSortLabel
                  className="transaction-sort-label"
                  active={sortConfig?.key === "amount"}
                  direction={
                    sortConfig?.key === "amount" && sortConfig.ascending
                      ? "asc"
                      : "desc"
                  }
                  onClick={(event) => onSort("amount", event)}
                >
                  Amount
                </TableSortLabel>
              </span>
              <span className="transaction-header-sort-clear">
                {sortConfig && (
                  <IconButton
                    size="small"
                    aria-label="Clear sorting"
                    onClick={onClearSort}
                  >
                    <CloseRounded />
                  </IconButton>
                )}
              </span>
            </Box>
            {transactions.map((transaction) =>
              editingId === transaction.id ? (
                <TransactionEditRow
                  key={transaction.id}
                  transaction={transaction}
                  categoryOptions={categoryOptions}
                  onCategoryChange={(categoryId) =>
                    onCategoryChange(transaction, categoryId)
                  }
                  onCancel={onCancelEdit}
                  onSave={(data, confirmPotentialDuplicate) =>
                    onSaveEdit(transaction.id, data, confirmPotentialDuplicate)
                  }
                />
              ) : (
                <TransactionRow
                  key={transaction.id}
                  transaction={transaction}
                  account={account}
                  categoryOptions={categoryOptions}
                  onCategoryChange={(categoryId) =>
                    onCategoryChange(transaction, categoryId)
                  }
                  onDelete={() => onDelete(transaction)}
                  deleting={deletingId === transaction.id}
                  successMessage={
                    successTransactionId === transaction.id
                      ? transaction.transferId
                        ? "Transfer updated successfully."
                        : "Transaction updated successfully."
                      : undefined
                  }
                  onEdit={() => onEdit(transaction)}
                  potentialDuplicate={transaction.potentialDuplicate}
                />
              ),
            )}
          </Box>
        )}
      </Collapse>
    </Box>
  );
}
