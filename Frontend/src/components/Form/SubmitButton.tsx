import { useFormContext } from "react-hook-form";
import { Button, CircularProgress } from "@mui/material";
import type { ButtonProps } from "@mui/material";
import type { ReactNode } from "react";

interface SubmitButtonProps extends Omit<ButtonProps, "type"> {
  children: ReactNode;
}

export function SubmitButton({ children, ...buttonProps }: SubmitButtonProps) {
  const { formState } = useFormContext();
  const isSubmitting = formState.isSubmitting;
  return (
    <Button
      type="submit"
      fullWidth
      variant="contained"
      disabled={isSubmitting}
      sx={{
        ...buttonProps.sx,
      }}
      {...buttonProps}
    >
      {isSubmitting ? <CircularProgress size={24} color="inherit" /> : children}
    </Button>
  );
}
