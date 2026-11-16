import { Box } from "@mui/material";
import { ConfirmDialog } from "@app/components/Dialog";
import type { AccountResponse } from "@app/services/accountsApi";
import type { TransactionResponse } from "@app/services/transactionApi";
import { formatSignedCurrency } from "@app/utils/transactions";

type TransactionDeleteDialogProps = {
  open: boolean;
  transaction: TransactionResponse | null;
  linkedTransactions: TransactionResponse[];
  accounts: AccountResponse[];
  loading: boolean;
  onConfirm: () => void;
  onUnlink: () => void;
  onCancel: () => void;
  onExited: () => void;
};

export function TransactionDeleteDialog({
  open,
  transaction,
  linkedTransactions,
  accounts,
  loading,
  onConfirm,
  onUnlink,
  onCancel,
  onExited,
}: TransactionDeleteDialogProps) {
  const isTransfer = Boolean(transaction?.transferId);

  return (
    <ConfirmDialog
      open={open}
      title={isTransfer ? "Delete transfer" : "Delete transaction"}
      message={
        isTransfer ? (
          <Box>
            <Box component="div">This transfer includes:</Box>
            <Box component="ul" sx={{ margin: "8px 0 0", paddingLeft: "20px" }}>
              {linkedTransactions.map((linkedTransaction) => {
                const account = accounts.find(
                  (candidate) => candidate.id === linkedTransaction.accountId,
                );
                const direction = linkedTransaction.amount < 0 ? "From" : "To";
                const formattedAmount = account
                  ? formatSignedCurrency(
                      linkedTransaction.amount,
                      account.currency,
                    )
                  : linkedTransaction.amount.toFixed(2);
                const displayAmount =
                  linkedTransaction.amount >= 0
                    ? `+${formattedAmount}`
                    : formattedAmount;

                return (
                  <li key={linkedTransaction.id}>
                    <strong>
                      {direction} {account?.name ?? "Unknown account"}
                    </strong>
                    {": "}
                    {linkedTransaction.description} ({displayAmount})
                  </li>
                );
              })}
            </Box>
            <Box className="transaction-delete-note">
              Unlink keeps both transactions. Delete removes them permanently.
            </Box>
          </Box>
        ) : (
          <>
            Are you sure you want to delete{" "}
            <strong>{transaction?.description}</strong>?
          </>
        )
      }
      confirmLabel={isTransfer ? "Delete transfer" : "Delete transaction"}
      secondaryLabel={isTransfer ? "Unlink transfer" : undefined}
      loading={loading}
      onConfirm={onConfirm}
      onSecondary={isTransfer ? onUnlink : undefined}
      onCancel={onCancel}
      onExited={onExited}
    />
  );
}
