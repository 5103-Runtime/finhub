import type { ComponentProps, ReactNode } from "react";
import { FormProvider, type FieldValues, type SubmitHandler, type UseFormReturn } from "react-hook-form";
import "./Form.css";

type FormProps<T extends FieldValues> = {
  form: UseFormReturn<T>;
  onSubmit: SubmitHandler<T>;
  children: ReactNode;
} & Omit<ComponentProps<"form">, "onSubmit" | "children">;

export function Form<T extends FieldValues>({ form, onSubmit, children, className,...formProps }: FormProps<T>) {
  return (
    <FormProvider {...form}>
      <form
        noValidate
        onSubmit={form.handleSubmit(onSubmit, (errors) => console.log("invalid:", errors))}
        className={["app-form", className].filter(Boolean).join(" ")}
        {...formProps}
      >
        {children}
      </form>
    </FormProvider>
  );
}