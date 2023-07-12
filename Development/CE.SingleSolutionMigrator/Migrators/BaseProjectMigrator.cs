// BaseProjectMigrator.cs
// Copyright (c) Kris Culin. All Rights Reserved.

using CE.SingleSolutionMigrator.Support;

namespace CE.SingleSolutionMigrator.Migrators
{
    public abstract class BaseProjectMigrator : BaseMigrator
    {
        #region Constructor
        protected BaseProjectMigrator(PerforceFacade p4)
            : base(p4)
        {
        }
        #endregion
    }
}
