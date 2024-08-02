using System;
using System.Collections.Generic;

namespace NirvedBackend.Entities;

public partial class UserNotice
{
    public int UserNoticeId { get; set; }

    public string Title { get; set; }

    public string Message { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public bool IsActive { get; set; }

    public DateTime CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }
}
