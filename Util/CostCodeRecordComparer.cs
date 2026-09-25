using StowellCoAPI.DTO;

namespace StowellCoApp.Util
{
    public class CostCodeRecordComparer : IEqualityComparer<CostCodeRecord>
    {
        public bool Equals(CostCodeRecord x, CostCodeRecord y)
        {
            // Assuming RecNum uniquely identifies a CostCodeRecord
            return x != null && y != null && x.RecNum == y.RecNum;
        }

        public int GetHashCode(CostCodeRecord obj)
        {
            return obj.RecNum.GetHashCode();
        }
    }
}
