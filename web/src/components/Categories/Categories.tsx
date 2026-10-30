import { useEffect, useState } from "react";
import {
  Alert,
  Box,
  Button,
  Card,
  CardContent,
  Container,
  Stack,
  Typography,
} from "@mui/material";
import AddRounded from "@mui/icons-material/AddRounded";
import DeleteOutlineRounded from "@mui/icons-material/DeleteOutlineRounded";
import EditRounded from "@mui/icons-material/EditRounded";
import Footer from "@app/components/Footer";
import Navbar from "@app/components/Navbar";
import { ConfirmDialog, FormDialog } from "@app/components/Dialog";
import { TextFormField } from "@app/components/Form";
import { CategoryChip } from "@app/components/Category/CategoryChip";
import {
  createCategory,
  deleteCategory,
  getCategories,
  updateCategory,
  type CategoryResponse,
} from "@app/services/categoriesApi";
import { applyFieldErrors, getErrorMessage } from "@app/utils/formErrors";
import {
  categorySchema,
  emptyCategoryForm,
  type CategoryFormData,
} from "@app/validators/categorySchemas";
import { useForm, type SubmitHandler } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import "@app/components/Accounts/Accounts.css";
import "./Categories.css";

export default function Categories() {
  const [categories, setCategories] = useState<CategoryResponse[]>([]);
  const [loading, setLoading] = useState(true);
  const [selectedCategory, setSelectedCategory] =
    useState<CategoryResponse | null>(null);
  const [categoryToDelete, setCategoryToDelete] =
    useState<CategoryResponse | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [loadError, setLoadError] = useState("");
  const [serverError, setServerError] = useState("");
  const form = useForm<CategoryFormData>({
    resolver: zodResolver(categorySchema),
    defaultValues: emptyCategoryForm,
    mode: "onTouched",
  });

  useEffect(() => {
    getCategories()
      .then(setCategories)
      .catch((error) =>
        setLoadError(getErrorMessage(error, "Unable to load categories.")),
      )
      .finally(() => setLoading(false));
  }, []);

  const openCreate = () => {
    setSelectedCategory(null);
    form.reset(emptyCategoryForm, { keepErrors: false });
    setServerError("");
    setIsModalOpen(true);
  };

  const openEdit = (category: CategoryResponse) => {
    setSelectedCategory(category);
    form.reset({ name: category.name }, { keepErrors: false });
    setServerError("");
    setIsModalOpen(true);
  };

  const onSubmit: SubmitHandler<CategoryFormData> = async (data) => {
    setServerError("");
    try {
      const saved = selectedCategory
        ? await updateCategory(selectedCategory.id, data)
        : await createCategory(data);
      setCategories((current) =>
        selectedCategory
          ? current.map((category) =>
              category.id === saved.id ? saved : category,
            )
          : [...current, saved],
      );
      setIsModalOpen(false);
    } catch (error) {
      applyFieldErrors(error, form.setError, ["name"]);
      setServerError(getErrorMessage(error, "Unable to save the category."));
    }
  };

  const handleDelete = async () => {
    if (!categoryToDelete) {
      return;
    }

    setDeleting(true);
    try {
      await deleteCategory(categoryToDelete.id);
      setCategories((current) =>
        current.filter((category) => category.id !== categoryToDelete.id),
      );
      setIsDeleteModalOpen(false);
    } catch (error) {
      setServerError(getErrorMessage(error, "Unable to delete the category."));
    } finally {
      setDeleting(false);
    }
  };

  const customCategories = categories.filter((category) => !category.isSystem);
  const systemCategories = categories.filter((category) => category.isSystem);
  const sections = [
    { title: "Your categories", items: customCategories },
    { title: "System categories", items: systemCategories },
  ];

  return (
    <Box className="categories-page">
      <Container maxWidth="xl" className="accounts-shell">
        <Navbar />
        <Box component="main">
          <Box className="accounts-welcome">
            <Box>
              <Typography className="accounts-kicker">YOUR SYSTEM</Typography>
              <Typography component="h1" className="accounts-title">
                Categories
              </Typography>
            </Box>
            <Button
              className="accounts-add-button"
              startIcon={<AddRounded />}
              onClick={openCreate}
            >
              Add category
            </Button>
          </Box>
          {loadError && (
            <Alert
              className="accounts-error"
              severity="error"
              variant="outlined"
            >
              {loadError}
            </Alert>
          )}
          {loading ? (
            <Typography className="accounts-empty">
              Loading categories...
            </Typography>
          ) : (
            <Stack spacing={3}>
              {sections.map((section) => (
                <Box component="section" key={section.title}>
                  <Typography component="h2" className="section-title">
                    {section.title}
                  </Typography>
                  <Stack spacing={1.5} sx={{ mt: 2 }}>
                    {section.items.map((category) => (
                      <Card
                        className="category-management-card"
                        key={category.id}
                      >
                        <CardContent>
                          <CategoryChip name={category.name} />
                          {!category.isSystem && (
                            <Box className="category-management-actions">
                              <Button
                                className="account-icon-button"
                                aria-label={`Edit ${category.name}`}
                                onClick={() => openEdit(category)}
                              >
                                <EditRounded />
                              </Button>
                              <Button
                                className="account-icon-button delete"
                                aria-label={`Delete ${category.name}`}
                                onClick={() => {
                                  setCategoryToDelete(category);
                                  setIsDeleteModalOpen(true);
                                }}
                              >
                                <DeleteOutlineRounded />
                              </Button>
                            </Box>
                          )}
                        </CardContent>
                      </Card>
                    ))}
                    {section.items.length === 0 && (
                      <Typography className="accounts-empty">
                        No custom categories yet.
                      </Typography>
                    )}
                  </Stack>
                </Box>
              ))}
            </Stack>
          )}
        </Box>
        <Footer />
      </Container>
      <FormDialog
        open={isModalOpen}
        title={selectedCategory ? "Edit category" : "Add a category"}
        form={form}
        onSubmit={onSubmit}
        onClose={() => setIsModalOpen(false)}
        onEntered={() => {
          form.setFocus("name");
          form.clearErrors("name");
        }}
        submitLabel={selectedCategory ? "Save category" : "Add category"}
        error={serverError}
      >
        <TextFormField<CategoryFormData>
          name="name"
          label="Category name"
          required
        />
      </FormDialog>
      <ConfirmDialog
        open={isDeleteModalOpen}
        title="Delete category"
        message={
          <>
            Are you sure you want to delete{" "}
            <strong>{categoryToDelete?.name}</strong>?
          </>
        }
        confirmLabel="Delete category"
        error={serverError}
        loading={deleting}
        onConfirm={handleDelete}
        onCancel={() => setIsDeleteModalOpen(false)}
        onExited={() => setCategoryToDelete(null)}
      />
    </Box>
  );
}
