import { useEffect } from "react";
import { useNavigate, Link as RouterLink } from "react-router-dom";
import { useAppDispatch, useAppSelector } from "../app/store/hooks";
import { loginUser } from "../app/store/slices/authSlice";
import { Container, Box, Typography, Link, Alert, Paper } from "@mui/material";
import { useForm, FormProvider } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { FormTextInput, SubmitButton } from "../components/Form";

/**
 * STEP 1: Define validation schema with Zod
 * This is where YOU define the rules for your form
 */
const loginSchema = z.object({
  email: z
    .string()
    .min(1, "Email is required")
    .email("Please enter a valid email address"),
  password: z
    .string()
    .min(6, "Password must be at least 6 characters")
    .max(50, "Password is too long"),
});

/**
 * STEP 2: Get TypeScript type from schema
 * This makes your code type-safe!
 */
type LoginFormData = z.infer<typeof loginSchema>;

const Login = () => {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  // Get auth state from Redux
  const { error, isAuthenticated } = useAppSelector((state) => state.auth);

  /**
   * STEP 3: Create react-hook-form instance
   * YOU control this - you can see exactly what's happening!
   */
  const methods = useForm<LoginFormData>({
    resolver: zodResolver(loginSchema), // Connect Zod validation
    defaultValues: {
      email: "",
      password: "",
    },
    mode: "onTouched", // Validate when field is touched (on blur or submit)
  });

  /**
   * STEP 4: Define submit handler
   * This is ONLY called if validation passes!
   */
  const handleLogin = async (data: LoginFormData) => {
    try {
      await dispatch(loginUser(data)).unwrap();
      console.log("[Login] Login successful!");
      // Navigate happens automatically via useEffect when isAuthenticated changes
    } catch (err) {
      console.error("[Login] Login failed:", err);
      // Error is shown via Redux state
    }
  };

  // Redirect to home if already authenticated
  useEffect(() => {
    if (isAuthenticated) {
      navigate("/");
    }
  }, [isAuthenticated, navigate]);

  return (
    <Container component="main" maxWidth="xs">
      <Box
        sx={{
          marginTop: 8,
          display: "flex",
          flexDirection: "column",
          alignItems: "center",
        }}
      >
        <Typography
          component="h1"
          variant="h4"
          sx={{ fontWeight: "bold", mb: 3 }}
        >
          Sign In
        </Typography>

        <Paper
          elevation={3}
          sx={{
            width: "100%",
            padding: 4,
            borderRadius: 2,
          }}
        >
          {/* STEP 5: Wrap form with FormProvider */}
          {/* This shares the form methods with all child components */}
          <FormProvider {...methods}>
            <Box
              component="form"
              onSubmit={methods.handleSubmit(handleLogin)}
              noValidate
            >
              {/* Show API errors */}
              {error && (
                <Alert severity="error" sx={{ mb: 2 }}>
                  {error}
                </Alert>
              )}

              {/* STEP 6: Use your reusable components! */}
              <FormTextInput
                name="email"
                type="email"
                label="Email Address"
                autoComplete="email"
                autoFocus
                required
              />

              <FormTextInput
                name="password"
                type="password"
                label="Password"
                autoComplete="current-password"
                required
              />

              {/* Reusable submit button */}
              <SubmitButton>Sign In</SubmitButton>
            </Box>
          </FormProvider>

          <Typography variant="body2" align="center" sx={{ mt: 3 }}>
            Don't have an account?{" "}
            <Link component={RouterLink} to="/register" variant="body2">
              Sign up
            </Link>
          </Typography>
        </Paper>
      </Box>
    </Container>
  );
};

export default Login;
