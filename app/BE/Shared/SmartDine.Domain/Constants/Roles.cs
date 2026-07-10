using SmartDine.Domain.Enums;
namespace SmartDine.Domain.Constants
{
    public static class Roles
    {
        public const string Customer = nameof(UserRole.CUSTOMER);
        public const string Guest = nameof(UserRole.GUEST);
        public const string Staff = nameof(UserRole.STAFF);
        public const string Chef = nameof(UserRole.CHEF);
        public const string Manager = nameof(UserRole.MANAGER);

        public const string AllDiners = Customer + "," + Guest;
        public const string AllDinersAndStaff = Customer + "," + Guest + "," + Staff;
        public const string KitchenStaff = Staff + "," + Chef + "," + Manager;
        public const string ManagerAndChef = Manager + "," + Chef;
        public const string StaffAndManager = Staff + "," + Manager;
        public const string CustomerAndManagement = Customer + "," + Staff + "," + Manager;
        public const string AllExceptChef = Customer + "," + Guest + "," + Staff + "," + Manager;
    }
}