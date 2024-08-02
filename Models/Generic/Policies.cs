namespace NirvedBackend.Models.Generic;

public enum Policies
{
    All=-1,
    Admin=0,
    SuperDistributor=1,
    Distributor=2,
    Retailer=3,
    AdminOrSuperDistributor=4,
    AdminOrDistributor=5,
    AdminOrRetailer=6,
    SuperDistributorOrDistributor=7,
    SuperDistributorOrRetailer=8,
    DistributorOrRetailer=9,
    AdminOrSuperDistributorOrDistributor=10,
    AdminOrSuperDistributorOrRetailer=11,
    AdminOrDistributorOrRetailer=12,
    SuperDistributorOrDistributorOrRetailer=13
}