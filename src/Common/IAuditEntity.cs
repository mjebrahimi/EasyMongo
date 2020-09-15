namespace EasyMongo
{
    public interface IAuditEntity
    {
        public string CreatedOn { get; set; } //CreatedAt - CreateDate, InsertDate
        public string UpdatedOn { get; set; } //UpdatedAt - UpdateDate
        public string DeletedOn { get; set; } //DeletedAt - DeletedDate

        public string CreatedBy { get; set; } //InsertUserId
        public string UpdatedBy { get; set; } //UpdateUserId
        public string DeletedBy { get; set; } //DeleteUserId
    }
}
