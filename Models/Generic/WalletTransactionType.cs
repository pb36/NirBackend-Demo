namespace NirvedBackend.Models.Generic;

public enum WalletTransactionType
{
    AdminTopUp=0,
    UserTopUp=1,
    JournalVoucher=2,
    CreditApproval=3,
    OutstandingClear=4,
    ProcessBill=5,
    RefundBill=6,
    ProcessBillList=7
}