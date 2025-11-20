import { useFormContext, Controller } from "react-hook-form";
import { Checkbox, FormControlLabel, Typography, Box } from "@mui/material";

/**
 * Form Checkbox Component
 *
 * For checkbox inputs with automatic validation and error display
 *
 * HOW TO USE:
 * <FormCheckbox name="agreeToTerms" label="I agree to terms & policy" />
 */

interface FormCheckboxProps {
  /**
   * Field name - must match your Zod schema
   */
  name: string;

  /**
   * Label text displayed next to the checkbox
   */
  label: string;
}

export function FormCheckbox({ name, label }: FormCheckboxProps) {
  const { control } = useFormContext();

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => {
        const { onChange, onBlur, value, ref } = field;
        const errorMessage = fieldState.error?.message;
        return (
          <Box sx={{ mt: 1, mb: 1 }}>
            <FormControlLabel
              control={
                <Checkbox
                  checked={!!value}
                  onChange={(e) => {
                    onChange(e.target.checked);
                  }}
                  onBlur={() => {
                    onBlur();
                  }}
                  inputRef={ref}
                  color={errorMessage ? "error" : "primary"}
                />
              }
              label={
                <Typography color={errorMessage ? "error" : "inherit"}>
                  {label}
                </Typography>
              }
            />
            {errorMessage && (
              <Typography
                color="error"
                variant="caption"
                display="block"
                sx={{ ml: 4, mt: -0.5 }}
              >
                {errorMessage}
              </Typography>
            )}
          </Box>
        );
      }}
    />
  );
}
