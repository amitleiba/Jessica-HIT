import { Component, OnInit, inject, OnDestroy } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from "@angular/forms";
import { TableModule } from "primeng/table";
import { DialogModule } from "primeng/dialog";
import { ButtonModule } from "primeng/button";
import { InputTextModule } from "primeng/inputtext";
import { DropdownModule } from "primeng/dropdown";
import { TagModule } from "primeng/tag";
import { AuthService } from "../../core/services/auth.service";
import { AlertService } from "../../core/services/alert.service";
import { ConfirmationService } from "primeng/api";
import { ConfirmDialogModule } from "primeng/confirmdialog";
import { Store } from "@ngrx/store";
import { authFeature } from "../../store/reducers/auth.reducer";
import { Subscription } from "rxjs";

interface RoleOption {
  label: string;
  value: string;
}

@Component({
  selector: "app-user-management",
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    FormsModule,
    TableModule,
    DialogModule,
    ButtonModule,
    InputTextModule,
    DropdownModule,
    TagModule,
    ConfirmDialogModule
  ],
  providers: [ConfirmationService],
  templateUrl: "./user-management.component.html",
  styleUrl: "./user-management.component.scss"
})
export class UserManagementComponent implements OnInit, OnDestroy {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly alertService = inject(AlertService);
  private readonly confirmationService = inject(ConfirmationService);
  private readonly store = inject(Store);

  users: any[] = [];
  loading = true;

  // Current logged-in user
  currentUserId: string | null = null;
  private userSub?: Subscription;

  // Role update tracking
  updatingUserRoles: { [userId: string]: boolean } = {};

  // Dialog state
  displayAddDialog = false;
  userForm!: FormGroup;
  submitting = false;

  roles: RoleOption[] = [
    { label: "Admin", value: "Admin" },
    { label: "Operator", value: "Operator" },
    { label: "Viewer", value: "Viewer" }
  ];

  ngOnInit(): void {
    this.initForm();
    this.loadUsers();
    this.userSub = this.store.select(authFeature.selectUser).subscribe(user => {
      this.currentUserId = user?.id ?? null;
    });
  }

  ngOnDestroy(): void {
    this.userSub?.unsubscribe();
  }

  initForm(): void {
    this.userForm = this.fb.group({
      username: ["", [Validators.required, Validators.minLength(3)]],
      email: ["", [Validators.required, Validators.email]],
      firstName: ["", [Validators.required, Validators.minLength(2)]],
      lastName: ["", [Validators.required, Validators.minLength(2)]],
      password: ["", [Validators.required, Validators.minLength(8)]],
      role: ["Viewer", Validators.required]
    });
  }

  loadUsers(): void {
    this.loading = true;
    this.authService.getUsers().subscribe({
      next: (users) => {
        // Extract primary role for dropdown binding
        this.users = users.map(u => ({
          ...u,
          role: u.roles?.[0] || "Viewer",
          _originalRole: u.roles?.[0] || "Viewer"
        }));
        this.loading = false;
      },
      error: (err) => {
        console.error("Failed to load users", err);
        this.alertService.danger("Failed to load users list.", "Error");
        this.loading = false;
      }
    });
  }

  openAddDialog(): void {
    this.userForm.reset({ role: "Viewer" });
    this.displayAddDialog = true;
  }

  closeAddDialog(): void {
    this.displayAddDialog = false;
  }

  onSubmit(): void {
    if (this.userForm.invalid) {
      return;
    }

    this.submitting = true;
    const requestData = this.userForm.value;

    this.authService.createUser(requestData).subscribe({
      next: (res) => {
        this.alertService.success(res.message || "User created successfully!", "Success");
        this.displayAddDialog = false;
        this.submitting = false;
        this.loadUsers();
      },
      error: (err) => {
        console.error("Failed to create user", err);
        this.alertService.danger(err || "Failed to create user.", "Creation Failed");
        this.submitting = false;
      }
    });
  }

  onRoleChange(user: any, newRole: string): void {
    const originalRole = user._originalRole;

    if (newRole === originalRole) {
      return;
    }

    this.confirmationService.confirm({
      message: `Change "${user.username}" role from "${originalRole}" to "${newRole}"?`,
      header: "Confirm Role Change",
      icon: "pi pi-user-edit",
      accept: () => {
        this.updatingUserRoles[user.id] = true;
        this.authService.updateUserRole(user.id, newRole).subscribe({
          next: (res) => {
            this.alertService.success(
              res.message || `Role updated to "${newRole}" successfully`,
              "Role Updated"
            );
            this.updatingUserRoles[user.id] = false;
            this.loadUsers();
          },
          error: (err) => {
            console.error("Failed to update role", err);
            this.alertService.danger(err || "Failed to update user role.", "Update Failed");
            // Revert the dropdown value
            user.role = originalRole;
            this.updatingUserRoles[user.id] = false;
          }
        });
      },
      reject: () => {
        // Revert the dropdown value
        user.role = originalRole;
      }
    });
  }

  deleteUser(userId: string, username: string): void {
    this.confirmationService.confirm({
      message: `Are you sure you want to delete user "${username}"? This action cannot be undone.`,
      header: 'Confirm Delete',
      icon: 'pi pi-exclamation-triangle',
      accept: () => {
        this.authService.deleteUser(userId).subscribe({
          next: (res) => {
            this.alertService.success(res.message || "User deleted successfully", "Deleted");
            this.loadUsers();
          },
          error: (err) => {
            console.error("Failed to delete user", err);
            this.alertService.danger(err || "Failed to delete user.", "Delete Failed");
          }
        });
      }
    });
  }

  getRoleSeverity(role: string): "success" | "info" | "warn" | "danger" | "secondary" | "contrast" {
    switch (role) {
      case "Admin":
        return "danger";
      case "Operator":
        return "info";
      case "Viewer":
        return "success";
      default:
        return "secondary";
    }
  }
}
