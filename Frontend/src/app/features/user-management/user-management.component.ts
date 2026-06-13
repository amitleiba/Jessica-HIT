import { Component, OnInit, inject } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule } from "@angular/forms";
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
export class UserManagementComponent implements OnInit {
  private readonly authService = inject(AuthService);
  private readonly fb = inject(FormBuilder);
  private readonly alertService = inject(AlertService);
  private readonly confirmationService = inject(ConfirmationService);

  users: any[] = [];
  loading = true;

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
        this.users = users;
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
