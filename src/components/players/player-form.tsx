"use client";

import { useState, FormEvent } from "react";
import { Button } from "@/components/ui/button";
import { Input, Label } from "@/components/ui/field";

export interface PlayerFormValues {
  displayName: string;
  firstName?: string | null;
  lastName?: string | null;
  email?: string | null;
}

export function PlayerForm({
  initialValues,
  onSubmit,
  onCancel,
}: {
  initialValues?: PlayerFormValues;
  onSubmit: (values: PlayerFormValues) => Promise<void>;
  onCancel: () => void;
}) {
  const [values, setValues] = useState<PlayerFormValues>({
    displayName: initialValues?.displayName ?? "",
    firstName: initialValues?.firstName ?? "",
    lastName: initialValues?.lastName ?? "",
    email: initialValues?.email ?? "",
  });
  const [submitting, setSubmitting] = useState(false);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setSubmitting(true);
    try {
      await onSubmit(values);
    } finally {
      setSubmitting(false);
    }
  }

  return (
    <form onSubmit={handleSubmit} className="flex flex-col gap-4">
      <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
        <div>
          <Label htmlFor="displayName">Anzeigename *</Label>
          <Input
            id="displayName"
            required
            value={values.displayName}
            onChange={(e) => setValues({ ...values, displayName: e.target.value })}
          />
        </div>
        <div>
          <Label htmlFor="email">E-Mail</Label>
          <Input
            id="email"
            type="email"
            value={values.email ?? ""}
            onChange={(e) => setValues({ ...values, email: e.target.value })}
          />
        </div>
        <div>
          <Label htmlFor="firstName">Vorname</Label>
          <Input
            id="firstName"
            value={values.firstName ?? ""}
            onChange={(e) => setValues({ ...values, firstName: e.target.value })}
          />
        </div>
        <div>
          <Label htmlFor="lastName">Nachname</Label>
          <Input
            id="lastName"
            value={values.lastName ?? ""}
            onChange={(e) => setValues({ ...values, lastName: e.target.value })}
          />
        </div>
      </div>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="ghost" onClick={onCancel}>
          Abbrechen
        </Button>
        <Button type="submit" disabled={submitting}>
          {submitting ? "Speichert…" : "Speichern"}
        </Button>
      </div>
    </form>
  );
}
