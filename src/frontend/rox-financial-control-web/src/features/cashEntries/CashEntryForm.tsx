import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { Plus } from "lucide-react";
import { useState } from "react";
import { useForm } from "react-hook-form";
import { z } from "zod";
import { api } from "../../api/client";
import type { CashEntryType } from "../../api/types";

const amountSchema = z
  .string()
  .trim()
  .min(1, "Informe o valor.")
  .transform((value, context) => {
    const compact = value.replace(/\s/g, "");
    const normalized = compact.includes(",")
      ? compact.replace(/\./g, "").replace(",", ".")
      : compact;
    const amount = Number(normalized);

    if (!Number.isFinite(amount)) {
      context.addIssue({
        code: "custom",
        message: "Informe um valor válido."
      });

      return z.NEVER;
    }

    return amount;
  })
  .pipe(z.number().positive("Informe um valor maior que zero."));

const schema = z.object({
  businessDate: z.string().min(1, "Informe a data."),
  type: z.enum(["Credit", "Debit"]),
  amount: amountSchema,
  description: z.string().trim().min(3, "Descreva o lançamento.").max(180)
});

type FormValues = z.input<typeof schema>;
type FormData = z.output<typeof schema>;

type CashEntryFormProps = {
  defaultDate: string;
  onCreated: () => void;
};

export function CashEntryForm({ defaultDate, onCreated }: CashEntryFormProps) {
  const queryClient = useQueryClient();
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    reset,
    formState: { errors }
  } = useForm<FormValues, unknown, FormData>({
    resolver: zodResolver(schema),
    defaultValues: {
      businessDate: defaultDate,
      type: "Credit",
      amount: "",
      description: ""
    }
  });

  function refreshQueries() {
    queryClient.invalidateQueries({ queryKey: ["cash-entries"] });
    queryClient.invalidateQueries({ queryKey: ["daily-balances"] });
    queryClient.invalidateQueries({ queryKey: ["outbox"] });
  }

  const mutation = useMutation({
    mutationFn: api.createCashEntry,
    onSuccess: () => {
      reset({
        businessDate: defaultDate,
        type: "Credit",
        amount: "",
        description: ""
      });
      setSuccessMessage("Lançamento salvo. A consolidação pode levar alguns segundos.");
      refreshQueries();
      window.setTimeout(refreshQueries, 1500);
      window.setTimeout(refreshQueries, 4000);
      window.setTimeout(refreshQueries, 8000);
      onCreated();
    }
  });

  function submit(data: FormData) {
    setSuccessMessage(null);
    mutation.mutate({
      businessDate: data.businessDate,
      type: data.type as CashEntryType,
      amount: data.amount,
      description: data.description,
      occurredAt: null
    });
  }

  return (
    <form className="entry-form" onSubmit={handleSubmit(submit)}>
      <label>
        <span>Data</span>
        <input type="date" {...register("businessDate")} />
        {errors.businessDate && <small>{errors.businessDate.message}</small>}
      </label>

      <label>
        <span>Tipo</span>
        <select {...register("type")}>
          <option value="Credit">Crédito</option>
          <option value="Debit">Débito</option>
        </select>
      </label>

      <label>
        <span>Valor</span>
        <input
          inputMode="decimal"
          placeholder="0,00"
          type="text"
          {...register("amount")}
        />
        {errors.amount && <small>{errors.amount.message}</small>}
      </label>

      <label>
        <span>Descrição</span>
        <textarea rows={4} maxLength={180} {...register("description")} />
        {errors.description && <small>{errors.description.message}</small>}
      </label>

      {mutation.error && <p className="form-error">{mutation.error.message}</p>}
      {successMessage && <p className="form-success">{successMessage}</p>}

      <button className="button button--primary" disabled={mutation.isPending} type="submit">
        <Plus size={18} />
        {mutation.isPending ? "Salvando" : "Registrar"}
      </button>
    </form>
  );
}
