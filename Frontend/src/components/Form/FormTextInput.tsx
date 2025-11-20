import { useFormContext, Controller } from "react-hook-form";
import { TextField, type TextFieldProps } from "@mui/material";

interface FormTextInputProps extends Omit<TextFieldProps, "name"> {
  name: string;
  type?: string;
  required?: boolean;
}

export function FormTextInput({
  name,
  type = "text",
  required = false,
  ...textFieldProps
}: FormTextInputProps) {
  const { control } = useFormContext();

  return (
    <Controller
      name={name}
      control={control}
      render={({ field, fieldState }) => {
        const { onChange, onBlur, value, ref } = field;
        const errorMessage = fieldState.error?.message;
        return (
          <TextField
            {...textFieldProps}
            name={name}
            value={value}
            inputRef={ref}
            type={type}
            error={!!errorMessage}
            helperText={errorMessage || textFieldProps.helperText}
            margin="normal"
            required={required}
            fullWidth
            onChange={(e) => {
              onChange(e); // Call react-hook-form's onChange
            }}
            onBlur={() => {
              onBlur(); // Call react-hook-form's onBlur
            }}
          />
        );
      }}
    />
  );
}
